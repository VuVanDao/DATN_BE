using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Modules.Task.Domain.Enum;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.Task.Application.Commands.AddCardDependency
{
    public class AddCardDependencyCommandHandler : IRequestHandler<AddCardDependencyCommand, CardDependencyItemDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly ICurrentUserService? _currentUserService;
        private readonly IHubContext<BoardHub>? _hubContext;
        private readonly ILogger<AddCardDependencyCommandHandler>? _logger;

        public AddCardDependencyCommandHandler(
            IApplicationDbContext context,
            IProjectAuthorizationService authorizationService,
            ICurrentUserService? currentUserService = null,
            IHubContext<BoardHub>? hubContext = null,
            ILogger<AddCardDependencyCommandHandler>? logger = null)
        {
            _context = context;
            _authorizationService = authorizationService;
            _currentUserService = currentUserService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<CardDependencyItemDto> Handle(AddCardDependencyCommand request, CancellationToken cancellationToken)
        {
            var blockedCardId = request.Direction == DependencyDirection.BlockedBy ? request.CardId : request.TargetCardId;
            var blockingCardId = request.Direction == DependencyDirection.BlockedBy ? request.TargetCardId : request.CardId;

            if (blockedCardId == blockingCardId)
            {
                throw new BadRequestException("Thẻ không thể tự tạo liên kết phụ thuộc với chính nó.");
            }

            var cards = await _context.Cards
                .Include(c => c.Column).ThenInclude(col => col.Board)
                .Include(c => c.Assignee)
                .Where(c => c.Id == blockedCardId || c.Id == blockingCardId)
                .ToListAsync(cancellationToken);

            if (cards.Count < 2)
            {
                throw new NotFoundException("Một trong hai thẻ công việc không tồn tại.");
            }

            var blockedCard = cards.First(c => c.Id == blockedCardId);
            var blockingCard = cards.First(c => c.Id == blockingCardId);

            if (blockedCard.Column.BoardId != blockingCard.Column.BoardId)
            {
                throw new BadRequestException("Không thể tạo quan hệ phụ thuộc giữa các thẻ thuộc các bảng (Board) khác nhau.");
            }

            var projectId = blockedCard.Column.Board.ProjectId;
            var boardId = blockedCard.Column.BoardId;
            await _authorizationService.EnsureCanModifyProjectAsync(projectId, cancellationToken);

            var existing = await _context.CardDependencies
                .AnyAsync(cd => cd.BlockedCardId == blockedCardId && cd.BlockingCardId == blockingCardId, cancellationToken);

            if (existing)
            {
                throw new BadRequestException("Mối quan hệ phụ thuộc giữa 2 thẻ này đã tồn tại.");
            }

            // Kiểm tra chu trình lặp vô hạn (Cycle Detection) trong phạm vi Board
            var hasCycle = await CheckCircularDependencyAsync(blockedCardId, blockingCardId, boardId, cancellationToken);
            if (hasCycle)
            {
                throw new BadRequestException("Không thể tạo liên kết vì sẽ tạo thành chu trình phụ thuộc vòng (Circular Dependency), gây tắc nghẽn công việc.");
            }

            var dependency = new CardDependency
            {
                BlockedCardId = blockedCardId,
                BlockingCardId = blockingCardId
            };

            _context.CardDependencies.Add(dependency);
            await _context.SaveChangesAsync(cancellationToken);

            var targetCard = request.Direction == DependencyDirection.BlockedBy ? blockingCard : blockedCard;
            var isDone = (targetCard.Column != null && targetCard.Column.IsDoneColumn) || targetCard.ProgressPercentage == 100;

            var result = new CardDependencyItemDto
            {
                DependencyId = dependency.Id,
                CardId = targetCard.Id,
                Title = targetCard.Title,
                ColumnTitle = targetCard.Column?.Title,
                IsDone = isDone,
                Type = targetCard.Type,
                Priority = targetCard.Priority,
                AssigneeName = targetCard.Assignee?.Username,
                AssigneeAvatarUrl = targetCard.Assignee?.AvatarUrl,
                Direction = request.Direction.ToString(),
                CreatedAt = dependency.CreatedAt
            };

            if (_hubContext != null)
            {
                try
                {
                    var currentUserId = _currentUserService?.UserId ?? Guid.Empty;
                    var message = new CardDependencyAddedMessage(
                        blockedCard.Column.BoardId,
                        dependency.Id,
                        blockedCardId,
                        blockingCardId,
                        currentUserId,
                        DateTimeOffset.UtcNow);

                    await _hubContext.Clients
                        .Group(BoardHub.GroupName(blockedCard.Column.BoardId))
                        .SendAsync(BoardHub.CardDependencyAdded, message, CancellationToken.None);

                    if (blockingCard.Column.BoardId != blockedCard.Column.BoardId)
                    {
                        await _hubContext.Clients
                            .Group(BoardHub.GroupName(blockingCard.Column.BoardId))
                            .SendAsync(BoardHub.CardDependencyAdded, message, CancellationToken.None);
                    }
                }
                catch (Exception exception)
                {
                    _logger?.LogWarning(exception, "Không thể phát sự kiện CardDependencyAdded cho board {BoardId}.", blockedCard.Column.BoardId);
                }
            }

            return result;
        }

        private async Task<bool> CheckCircularDependencyAsync(
            Guid blockedCardId,
            Guid blockingCardId,
            Guid boardId,
            CancellationToken cancellationToken)
        {
            // Nếu ta thêm cạnh: blockingCardId -> blockedCardId
            // Chu trình xảy ra nếu ĐÃ CÓ đường đi từ blockedCardId tới blockingCardId
            var allDependencies = await _context.CardDependencies
                .AsNoTracking()
                .Where(cd => cd.BlockedCard.Column.BoardId == boardId)
                .Select(cd => new { cd.BlockingCardId, cd.BlockedCardId })
                .ToListAsync(cancellationToken);

            var adjList = new Dictionary<Guid, List<Guid>>();
            foreach (var dep in allDependencies)
            {
                if (!adjList.TryGetValue(dep.BlockingCardId, out var list))
                {
                    list = new List<Guid>();
                    adjList[dep.BlockingCardId] = list;
                }
                list.Add(dep.BlockedCardId);
            }

            // BFS tìm đường từ blockedCardId tới blockingCardId
            var queue = new Queue<Guid>();
            var visited = new HashSet<Guid>();

            queue.Enqueue(blockedCardId);
            visited.Add(blockedCardId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == blockingCardId)
                {
                    return true;
                }

                if (adjList.TryGetValue(current, out var neighbors))
                {
                    foreach (var neighbor in neighbors)
                    {
                        if (visited.Add(neighbor))
                        {
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            return false;
        }
    }
}
