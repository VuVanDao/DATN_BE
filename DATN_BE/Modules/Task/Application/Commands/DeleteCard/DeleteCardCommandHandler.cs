using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Task.Application.Commands.DeleteCard
{
    public class DeleteCardCommandHandler : IRequestHandler<DeleteCardCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;

        public DeleteCardCommandHandler(IApplicationDbContext context, IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }

        public async Task<bool> Handle(DeleteCardCommand request, CancellationToken cancellationToken)
        {
            var targetedCard = await _context.Cards
                .Include(card => card.Column).ThenInclude(column => column.Board)
                .FirstOrDefaultAsync(card => card.Id == request.CardId, cancellationToken)
                ?? throw new NotFoundException("Card", request.CardId);

            await _authorizationService.EnsureCanModifyProjectAsync(targetedCard.Column.Board.ProjectId, cancellationToken);

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
            return true;
        }
    }
}
