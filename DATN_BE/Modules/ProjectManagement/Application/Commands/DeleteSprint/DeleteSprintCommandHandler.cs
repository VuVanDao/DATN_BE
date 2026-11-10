using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteSprint
{
    public class DeleteSprintCommandHandler : IRequestHandler<DeleteSprintCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly IHubContext<BoardHub>? _hubContext;
        private readonly ILogger<DeleteSprintCommandHandler>? _logger;

        public DeleteSprintCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IProjectAuthorizationService authorizationService,
            IHubContext<BoardHub>? hubContext = null,
            ILogger<DeleteSprintCommandHandler>? logger = null)
        {
            _context = context;
            _currentUserService = currentUserService;
            _authorizationService = authorizationService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteSprintCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để xóa Sprint.");

            var sprint = await _context.Sprints
                .Include(s => s.Cards)
                .FirstOrDefaultAsync(s => s.Id == request.SprintId, cancellationToken)
                ?? throw new NotFoundException("Sprint", request.SprintId);

            await _authorizationService.EnsureCanManageProjectAsync(sprint.ProjectId, cancellationToken);

            var boardId = sprint.BoardId;
            var sprintId = sprint.Id;

            // Các thẻ trong Sprint được trả về Backlog (SprintId = null)
            foreach (var card in sprint.Cards)
            {
                card.SprintId = null;
            }

            _context.Sprints.Remove(sprint);
            await _context.SaveChangesAsync(cancellationToken);

            if (_hubContext != null)
            {
                try
                {
                    await _hubContext.Clients
                        .Group(BoardHub.GroupName(boardId))
                        .SendAsync(
                            BoardHub.SprintDeleted,
                            new SprintDeletedMessage(
                                boardId,
                                sprintId,
                                currentUserId,
                                DateTimeOffset.UtcNow),
                            CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger?.LogWarning(exception, "Không thể phát sự kiện SprintDeleted cho board {BoardId}.", boardId);
                }
            }

            return true;
        }
    }
}
