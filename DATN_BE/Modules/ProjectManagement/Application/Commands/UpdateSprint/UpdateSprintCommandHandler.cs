using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateSprint
{
    public class UpdateSprintCommandHandler : IRequestHandler<UpdateSprintCommand, SprintDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly IHubContext<BoardHub>? _hubContext;
        private readonly ILogger<UpdateSprintCommandHandler>? _logger;

        public UpdateSprintCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IProjectAuthorizationService authorizationService,
            IHubContext<BoardHub>? hubContext = null,
            ILogger<UpdateSprintCommandHandler>? logger = null)
        {
            _context = context;
            _currentUserService = currentUserService;
            _authorizationService = authorizationService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<SprintDto> Handle(UpdateSprintCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để cập nhật Sprint.");

            var sprint = await _context.Sprints
                .Include(s => s.Cards)
                    .ThenInclude(c => c.Column)
                .FirstOrDefaultAsync(s => s.Id == request.SprintId, cancellationToken)
                ?? throw new NotFoundException("Sprint", request.SprintId);

            await _authorizationService.EnsureCanManageProjectAsync(sprint.ProjectId, cancellationToken);

            sprint.Name = request.Name.Trim();
            sprint.Goal = request.Goal?.Trim();
            if (request.StartDate.HasValue)
            {
                sprint.StartDate = request.StartDate.Value;
            }
            if (request.EndDate.HasValue)
            {
                sprint.EndDate = request.EndDate.Value;
            }

            await _context.SaveChangesAsync(cancellationToken);

            var totalCards = sprint.Cards.Count;
            var completedCards = sprint.Cards.Count(c => (c.Column != null && c.Column.IsDoneColumn) || c.IsCompleted);
            var totalStoryPoints = sprint.Cards.Sum(c => c.StoryPoints ?? 0);
            var completedStoryPoints = sprint.Cards
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
                            BoardHub.SprintUpdated,
                            new SprintUpdatedMessage(
                                sprint.BoardId,
                                result,
                                currentUserId,
                                DateTimeOffset.UtcNow),
                            CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger?.LogWarning(exception, "Không thể phát sự kiện SprintUpdated cho board {BoardId}.", sprint.BoardId);
                }
            }

            return result;
        }
    }
}
