using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Task.Application.Commands.DeleteCard
{
    public class DeleteCardCommandHandler : IRequestHandler<DeleteCardCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        public DeleteCardCommandHandler(IApplicationDbContext context)
        {
            this._context = context;
        }
        public async Task<bool> Handle(DeleteCardCommand request, CancellationToken cancellationToken)
        {
            var targetedCard = await _context.Cards
                .FirstOrDefaultAsync(card => card.Id == request.CardId, cancellationToken) ?? throw new NotFoundException("Card", request.CardId);
            _context.Cards.Remove(targetedCard);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
