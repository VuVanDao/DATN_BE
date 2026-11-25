using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.CompleteSprint
{
    public class CompleteSprintCommandHandler : IRequestHandler<CompleteSprintCommand, SprintDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly IHubContext<BoardHub>? _hubContext;
        private readonly ILogger<CompleteSprintCommandHandler>? _logger;

        public CompleteSprintCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IProjectAuthorizationService authorizationService,
            IHubContext<BoardHub>? hubContext = null,
            ILogger<CompleteSprintCommandHandler>? logger = null)
        {
            _context = context;
            _currentUserService = currentUserService;
            _authorizationService = authorizationService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<SprintDto> Handle(CompleteSprintCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new UnauthorizedException("Bạn cần đăng nhập để hoàn thành Sprint.");

            var sprint = await _context.Sprints
                .Include(s => s.Cards)
                    .ThenInclude(c => c.Column)
                .FirstOrDefaultAsync(s => s.Id == request.SprintId, cancellationToken)
                ?? throw new NotFoundException("Sprint", request.SprintId);

            await _authorizationService.EnsureCanManageProjectAsync(sprint.ProjectId, cancellationToken);

            if (sprint.Status != SprintStatus.Active)
            {
                throw new BadRequestException("Chỉ có thể hoàn thành Sprint đang ở trạng thái chạy (Active).");
            }

            if (request.MoveUnfinishedToSprintId.HasValue)
            {
                var targetSprintExists = await _context.Sprints
                    .AsNoTracking()
                    .AnyAsync(s => s.Id == request.MoveUnfinishedToSprintId.Value &&
                                   s.ProjectId == sprint.ProjectId &&
                                   s.Status == SprintStatus.Future, cancellationToken);

                if (!targetSprintExists)
                {
                    throw new BadRequestException("Sprint đích để chuyển thẻ chưa hoàn thành không hợp lệ hoặc không ở trạng thái Future.");
                }
            }

            // Tách các task chưa hoàn thành (không ở cột Done và chưa được đánh dấu hoàn thành)
            var unfinishedCards = sprint.Cards
                .Where(c => !((c.Column != null && c.Column.IsDoneColumn) || c.IsCompleted))
                .ToList();

            foreach (var card in unfinishedCards)
            {
                card.SprintId = request.MoveUnfinishedToSprintId;
            }

            sprint.Status = SprintStatus.Closed;
            await _context.SaveChangesAsync(cancellationToken);

            var remainingCards = sprint.Cards.Where(c => c.SprintId == sprint.Id).ToList();
            var totalCards = remainingCards.Count;
            var completedCards = remainingCards.Count(c => (c.Column != null && c.Column.IsDoneColumn) || c.IsCompleted);
            var totalStoryPoints = remainingCards.Sum(c => c.StoryPoints ?? 0);
            var completedStoryPoints = remainingCards
                .Where(c => (c.Column != null && c.Column.IsDoneColumn) || c.IsCompleted)
                .Sum(c => c.StoryPoints ?? 0);

            var result = new SprintDto
            {
                Id = sprint.Id,
                ProjectId = sprint.ProjectId,
                BoardId = sprint.BoardId,
                Name = sprint.Name,
                Goal = sprint.Goal,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                Status = sprint.Status,
                CreatedAt = sprint.CreatedAt,
                TotalCards = totalCards,
                CompletedCards = completedCards,
                TotalStoryPoints = totalStoryPoints,
                CompletedStoryPoints = completedStoryPoints
            };

            if (_hubContext != null)
            {
                try
                {
                    await _hubContext.Clients
                        .Group(BoardHub.GroupName(sprint.BoardId))
                        .SendAsync(
                            BoardHub.SprintCompleted,
                            new SprintCompletedMessage(
                                sprint.BoardId,
                                sprint.Id,
                                currentUserId,
                                DateTimeOffset.UtcNow),
                            CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger?.LogWarning(exception, "Không thể phát sự kiện SprintCompleted cho board {BoardId}.", sprint.BoardId);
                }
            }

            return result;
        }
    }
}
