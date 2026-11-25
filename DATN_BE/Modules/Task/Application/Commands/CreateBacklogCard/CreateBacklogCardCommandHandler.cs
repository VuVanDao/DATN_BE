using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;
using DATN_BE.Modules.Security.Domain.Enum;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Shared.Application.Events;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.Task.Application.Commands.CreateBacklogCard
{
    public class CreateBacklogCardCommandHandler : IRequestHandler<CreateBacklogCardCommand, CardDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly IHubContext<BoardHub>? _hubContext;
        private readonly ILogger<CreateBacklogCardCommandHandler>? _logger;
        private readonly IMediator? _mediator;

        public CreateBacklogCardCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IProjectAuthorizationService authorizationService,
            IHubContext<BoardHub>? hubContext = null,
            ILogger<CreateBacklogCardCommandHandler>? logger = null,
            IMediator? mediator = null)
        {
            _context = context;
            _currentUserService = currentUserService;
            _authorizationService = authorizationService;
            _hubContext = hubContext;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<CardDto> Handle(CreateBacklogCardCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new UnauthorizedException("Bạn cần đăng nhập để tạo thẻ công việc.");

            // 1. Tìm Bảng Scrum chỉ định để gán Cột cho thẻ
            var scrumBoard = await _context.Boards
                .Include(b => b.Columns)
                .FirstOrDefaultAsync(b => b.Id == request.BoardId
                                       && b.Type == BoardType.Scrum, cancellationToken)
                ?? throw new NotFoundException("Bảng Scrum không tồn tại.");

            await _authorizationService.EnsureCanModifyProjectAsync(scrumBoard.ProjectId, cancellationToken);

            var reporter = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken)
                ?? throw new NotFoundException("Người dùng không tồn tại.");

            if (!scrumBoard.Columns.Any())
            {
                throw new BadRequestException("Bảng Scrum chưa có Cột để chứa công việc.");
            }

            var targetColumn = scrumBoard.Columns
                .Where(col => !col.IsDoneColumn)
                .OrderBy(col => col.Position)
                .FirstOrDefault() ?? scrumBoard.Columns.OrderBy(col => col.Position).First();

            // 2. Nếu chỉ định SprintId, kiểm tra tính hợp lệ của Sprint
            if (request.SprintId.HasValue)
            {
                var sprint = await _context.Sprints
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == request.SprintId.Value && s.BoardId == scrumBoard.Id, cancellationToken)
                    ?? throw new NotFoundException("Sprint không tồn tại trên Bảng Scrum này.");

                if (sprint.Status == SprintStatus.Closed)
                {
                    throw new BadRequestException("Không thể tạo công việc vào một Sprint đã hoàn thành.");
                }
            }

            // 3. Tính Position tiếp theo trong Cột
            var maxPosition = await _context.Cards
                .Where(c => c.ColumnId == targetColumn.Id)
                .MaxAsync(c => (int?)c.Position, cancellationToken) ?? -1;

            // 4. Tạo thẻ mới cho Scrum
            var newCard = new Card
            {
                ColumnId = targetColumn.Id,
                Title = request.Title.Trim(),
                Description = request.Description,
                Position = maxPosition + 1,
                Type = request.Type,
                Priority = request.Priority,
                StoryPoints = request.StoryPoints,
                SprintId = request.SprintId,
                ReporterId = currentUserId,
                IsCompleted = false
            };

            _context.Cards.Add(newCard);
            await _context.SaveChangesAsync(cancellationToken);

            var result = new CardDto
            {
                Id = newCard.Id,
                Title = newCard.Title,
                Description = newCard.Description,
                Position = newCard.Position,
                Type = newCard.Type,
                Priority = newCard.Priority,
                StoryPoints = newCard.StoryPoints,
                StartDate = newCard.StartDate,
                DueDate = newCard.DueDate,
                IsCompleted = newCard.IsCompleted,
                ColumnId = newCard.ColumnId,
                ReporterId = newCard.ReporterId,
                ReporterName = reporter.Username,
                ReporterAvatarUrl = reporter.AvatarUrl,
                SprintId = newCard.SprintId,
                CreatedAt = newCard.CreatedAt,
                CommentsCount = 0
            };

            if (_hubContext != null)
            {
                try
                {
                    await _hubContext.Clients
                        .Group(BoardHub.GroupName(scrumBoard.Id))
                        .SendAsync(
                            BoardHub.CardCreated,
                            new CardCreatedMessage(
                                scrumBoard.Id,
                                result,
                                currentUserId,
                                DateTimeOffset.UtcNow),
                            CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger?.LogWarning(exception, "Không thể phát sự kiện CardCreated cho board {BoardId}.", scrumBoard.Id);
                }
            }

            if (_mediator != null)
            {
                await _mediator.Publish(new EntityActivityLoggedEvent(
                    currentUserId,
                    ActivityEntityType.Card,
                    newCard.Id,
                    ActivityActionType.Created,
                    new
                    {
                        cardTitle = newCard.Title,
                        columnId = newCard.ColumnId,
                        boardId = scrumBoard.Id,
                        isBacklog = true
                    }), cancellationToken);
            }

            return result;
        }
    }
}
