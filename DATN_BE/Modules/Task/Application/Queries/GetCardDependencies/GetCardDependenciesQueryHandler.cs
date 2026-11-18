using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Task.Application.Queries.GetCardDependencies
{
    public class GetCardDependenciesQueryHandler : IRequestHandler<GetCardDependenciesQuery, CardDependenciesResultDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;

        public GetCardDependenciesQueryHandler(
            IApplicationDbContext context,
            IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }

        public async Task<CardDependenciesResultDto> Handle(GetCardDependenciesQuery request, CancellationToken cancellationToken)
        {
            var card = await _context.Cards
                .AsNoTracking()
                .Include(c => c.Column).ThenInclude(col => col.Board)
                .FirstOrDefaultAsync(c => c.Id == request.CardId, cancellationToken)
                ?? throw new NotFoundException("Thẻ công việc không tồn tại.");

            await _authorizationService.EnsureCanViewProjectAsync(card.Column.Board.ProjectId, cancellationToken);

            // 1. Lấy danh sách các thẻ đang CHẶN thẻ này (BlockedBy: Thẻ này là BlockedCard, cần lấy BlockingCard)
            var blockedBy = await _context.CardDependencies
                .AsNoTracking()
                .Where(cd => cd.BlockedCardId == request.CardId)
                .Select(cd => new CardDependencyItemDto
                {
                    DependencyId = cd.Id,
                    CardId = cd.BlockingCard.Id,
                    Title = cd.BlockingCard.Title,
                    ColumnTitle = cd.BlockingCard.Column != null ? cd.BlockingCard.Column.Title : null,
                    IsDone = (cd.BlockingCard.Column != null && cd.BlockingCard.Column.IsDoneColumn) || cd.BlockingCard.IsCompleted,
                    Type = cd.BlockingCard.Type,
                    Priority = cd.BlockingCard.Priority,
                    AssigneeName = cd.BlockingCard.Assignee != null ? cd.BlockingCard.Assignee.Username : null,
                    AssigneeAvatarUrl = cd.BlockingCard.Assignee != null ? cd.BlockingCard.Assignee.AvatarUrl : null,
                    Direction = "BlockedBy",
                    CreatedAt = cd.CreatedAt
                })
                .ToListAsync(cancellationToken);

            // 2. Lấy danh sách các thẻ ĐANG BỊ thẻ này chặn (Blocking: Thẻ này là BlockingCard, cần lấy BlockedCard)
            var blocking = await _context.CardDependencies
                .AsNoTracking()
                .Where(cd => cd.BlockingCardId == request.CardId)
                .Select(cd => new CardDependencyItemDto
                {
                    DependencyId = cd.Id,
                    CardId = cd.BlockedCard.Id,
                    Title = cd.BlockedCard.Title,
                    ColumnTitle = cd.BlockedCard.Column != null ? cd.BlockedCard.Column.Title : null,
                    IsDone = (cd.BlockedCard.Column != null && cd.BlockedCard.Column.IsDoneColumn) || cd.BlockedCard.IsCompleted,
                    Type = cd.BlockedCard.Type,
                    Priority = cd.BlockedCard.Priority,
                    AssigneeName = cd.BlockedCard.Assignee != null ? cd.BlockedCard.Assignee.Username : null,
                    AssigneeAvatarUrl = cd.BlockedCard.Assignee != null ? cd.BlockedCard.Assignee.AvatarUrl : null,
                    Direction = "Blocking",
                    CreatedAt = cd.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new CardDependenciesResultDto
            {
                BlockedBy = blockedBy,
                Blocking = blocking
            };
        }
    }
}
