using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.StartSprint
{
    public class StartSprintCommandHandler : IRequestHandler<StartSprintCommand, SprintDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly IHubContext<BoardHub>? _hubContext;
        private readonly ILogger<StartSprintCommandHandler>? _logger;

        public StartSprintCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IProjectAuthorizationService authorizationService,
            IHubContext<BoardHub>? hubContext = null,
            ILogger<StartSprintCommandHandler>? logger = null)
        {
            _context = context;
            _currentUserService = currentUserService;
            _authorizationService = authorizationService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<SprintDto> Handle(StartSprintCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để bắt đầu Sprint.");

            var sprint = await _context.Sprints
                .Include(s => s.Cards)
                    .ThenInclude(c => c.Column)
                .FirstOrDefaultAsync(s => s.Id == request.SprintId, cancellationToken)
                ?? throw new NotFoundException("Sprint", request.SprintId);

            await _authorizationService.EnsureCanManageProjectAsync(sprint.ProjectId, cancellationToken);

            if (sprint.Status == SprintStatus.Active)
            {
                throw new BadRequestException("Sprint này hiện đang chạy.");
            }

            if (sprint.Status == SprintStatus.Closed)
            {
                throw new BadRequestException("Sprint này đã hoàn thành, không thể bắt đầu lại.");
            }

            // Kiểm tra trong Bảng Scrum này đã có Sprint nào đang Active chưa
            var hasActiveSprint = await _context.Sprints
                .AsNoTracking()
                .AnyAsync(s => s.BoardId == sprint.BoardId && s.Status == SprintStatus.Active && s.Id != sprint.Id, cancellationToken);

            if (hasActiveSprint)
            {
                throw new BadRequestException("Bảng Scrum này hiện đã có một Sprint đang chạy. Vui lòng hoàn thành Sprint hiện tại trước khi bắt đầu Sprint mới.");
            }

            var startDate = request.StartDate ?? (sprint.StartDate != default ? sprint.StartDate : DateTimeOffset.UtcNow);
            var endDate = request.EndDate ?? (sprint.EndDate != default ? sprint.EndDate : startDate.AddDays(14));

            sprint.Status = SprintStatus.Active;
            sprint.StartDate = startDate;
            sprint.EndDate = endDate;
            if (!string.IsNullOrWhiteSpace(request.Goal))
            {
                sprint.Goal = request.Goal.Trim();
            }

            await _context.SaveChangesAsync(cancellationToken);

            var totalCards = sprint.Cards.Count;
            var completedCards = sprint.Cards.Count(c => (c.Column != null && c.Column.IsDoneColumn) || c.ProgressPercentage == 100);
            var totalStoryPoints = sprint.Cards.Sum(c => c.StoryPoints ?? 0);
            var completedStoryPoints = sprint.Cards
                .Where(c => (c.Column != null && c.Column.IsDoneColumn) || c.ProgressPercentage == 100)
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
                            BoardHub.SprintStarted,
                            new SprintStartedMessage(
                                sprint.BoardId,
                                result,
                                currentUserId,
                                DateTimeOffset.UtcNow),
                            CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger?.LogWarning(exception, "Không thể phát sự kiện SprintStarted cho board {BoardId}.", sprint.BoardId);
                }
            }

            return result;
        }
    }
}
