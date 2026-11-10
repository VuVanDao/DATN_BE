using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.Task.Application.Commands.DeleteCard
{
    public class DeleteCardCommandHandler : IRequestHandler<DeleteCardCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly IHubContext<BoardHub>? _hubContext;
        private readonly ICurrentUserService? _currentUserService;
        private readonly ILogger<DeleteCardCommandHandler>? _logger;

        public DeleteCardCommandHandler(
            IApplicationDbContext context,
            IProjectAuthorizationService authorizationService,
            IHubContext<BoardHub>? hubContext = null,
            ICurrentUserService? currentUserService = null,
            ILogger<DeleteCardCommandHandler>? logger = null)
        {
            _context = context;
            _authorizationService = authorizationService;
            _hubContext = hubContext;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteCardCommand request, CancellationToken cancellationToken)
        {
            var targetedCard = await _context.Cards
                .Include(card => card.Column).ThenInclude(column => column.Board)
                .FirstOrDefaultAsync(card => card.Id == request.CardId, cancellationToken)
                ?? throw new NotFoundException("Card", request.CardId);

            await _authorizationService.EnsureCanModifyProjectAsync(targetedCard.Column.Board.ProjectId, cancellationToken);

            var boardId = targetedCard.Column.BoardId;
            var cardId = targetedCard.Id;
            var sourceColumnId = targetedCard.ColumnId;

            _context.Cards.Remove(targetedCard);

            var cardComments = await _context.CardComments
                .Where(c => c.CardId == targetedCard.Id)
                .ToListAsync(cancellationToken);

            if (cardComments.Count > 0)
            {
                _context.CardComments.RemoveRange(cardComments);
            }

            var dependencies = await _context.CardDependencies
                .Where(cd => cd.BlockedCardId == targetedCard.Id || cd.BlockingCardId == targetedCard.Id)
                .ToListAsync(cancellationToken);

            if (dependencies.Count > 0)
            {
                _context.CardDependencies.RemoveRange(dependencies);
            }

            var remainingCards = await _context.Cards
                .Where(c => c.ColumnId == sourceColumnId && c.Id != targetedCard.Id)
                .OrderBy(c => c.Position)
                .ToListAsync(cancellationToken);

            for (int i = 0; i < remainingCards.Count; i++)
            {
                remainingCards[i].Position = i;
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (_hubContext != null)
            {
                try
                {
                    await _hubContext.Clients
                        .Group(BoardHub.GroupName(boardId))
                        .SendAsync(
                            BoardHub.CardDeleted,
                            new CardDeletedMessage(
                                boardId,
                                cardId,
                                sourceColumnId,
                                _currentUserService?.UserId ?? Guid.Empty,
                                DateTimeOffset.UtcNow),
                            CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger?.LogWarning(exception, "Không thể phát sự kiện CardDeleted cho board {BoardId}.", boardId);
                }
            }

            return true;
        }
    }
}
