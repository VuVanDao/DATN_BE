using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.Task.Application.Commands.RemoveCardDependency
{
    public class RemoveCardDependencyCommandHandler : IRequestHandler<RemoveCardDependencyCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly ICurrentUserService? _currentUserService;
        private readonly IHubContext<BoardHub>? _hubContext;
        private readonly ILogger<RemoveCardDependencyCommandHandler>? _logger;

        public RemoveCardDependencyCommandHandler(
            IApplicationDbContext context,
            IProjectAuthorizationService authorizationService,
            ICurrentUserService? currentUserService = null,
            IHubContext<BoardHub>? hubContext = null,
            ILogger<RemoveCardDependencyCommandHandler>? logger = null)
        {
            _context = context;
            _authorizationService = authorizationService;
            _currentUserService = currentUserService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<bool> Handle(RemoveCardDependencyCommand request, CancellationToken cancellationToken)
        {
            var dependency = await _context.CardDependencies
                .Include(cd => cd.BlockedCard).ThenInclude(c => c.Column).ThenInclude(col => col.Board)
                .Include(cd => cd.BlockingCard).ThenInclude(c => c.Column)
                .FirstOrDefaultAsync(cd => cd.Id == request.DependencyId, cancellationToken)
                ?? throw new NotFoundException("Quan hệ phụ thuộc không tồn tại.");

            var projectId = dependency.BlockedCard.Column.Board.ProjectId;
            await _authorizationService.EnsureCanModifyProjectAsync(projectId, cancellationToken);

            var boardId = dependency.BlockedCard.Column.BoardId;
            var blockedCardId = dependency.BlockedCardId;
            var blockingCardId = dependency.BlockingCardId;
            var otherBoardId = dependency.BlockingCard.Column?.BoardId;

            _context.CardDependencies.Remove(dependency);
            await _context.SaveChangesAsync(cancellationToken);

            if (_hubContext != null)
            {
                try
                {
                    var currentUserId = _currentUserService?.UserId ?? Guid.Empty;
                    var message = new CardDependencyRemovedMessage(
                        boardId,
                        request.DependencyId,
                        blockedCardId,
                        blockingCardId,
                        currentUserId,
                        DateTimeOffset.UtcNow);

                    await _hubContext.Clients
                        .Group(BoardHub.GroupName(boardId))
                        .SendAsync(BoardHub.CardDependencyRemoved, message, CancellationToken.None);

                    if (otherBoardId.HasValue && otherBoardId.Value != boardId)
                    {
                        await _hubContext.Clients
                            .Group(BoardHub.GroupName(otherBoardId.Value))
                            .SendAsync(BoardHub.CardDependencyRemoved, message, CancellationToken.None);
                    }
                }
                catch (Exception exception)
                {
                    _logger?.LogWarning(exception, "Không thể phát sự kiện CardDependencyRemoved cho board {BoardId}.", boardId);
                }
            }

            return true;
        }
    }
}
