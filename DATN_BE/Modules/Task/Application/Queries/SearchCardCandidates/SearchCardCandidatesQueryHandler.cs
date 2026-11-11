using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Task.Application.Queries.SearchCardCandidates
{
    public class SearchCardCandidatesQueryHandler : IRequestHandler<SearchCardCandidatesQuery, List<CardCandidateDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;

        public SearchCardCandidatesQueryHandler(
            IApplicationDbContext context,
            IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }

        public async Task<List<CardCandidateDto>> Handle(SearchCardCandidatesQuery request, CancellationToken cancellationToken)
        {
            var card = await _context.Cards
                .AsNoTracking()
                .Include(c => c.Column).ThenInclude(col => col.Board)
                .FirstOrDefaultAsync(c => c.Id == request.CardId, cancellationToken)
                ?? throw new NotFoundException("Thẻ công việc không tồn tại.");

            var boardId = card.Column.BoardId;
            var projectId = card.Column.Board.ProjectId;
            await _authorizationService.EnsureCanViewProjectAsync(projectId, cancellationToken);

            // Các thẻ đã có liên kết với thẻ hiện tại (đã chặn hoặc đang bị chặn)
            var linkedCardIds = await _context.CardDependencies
                .AsNoTracking()
                .Where(cd => cd.BlockedCardId == request.CardId || cd.BlockingCardId == request.CardId)
                .Select(cd => cd.BlockedCardId == request.CardId ? cd.BlockingCardId : cd.BlockedCardId)
                .ToListAsync(cancellationToken);

            var query = _context.Cards
                .AsNoTracking()
                .Include(c => c.Column)
                .Include(c => c.Assignee)
                .Where(c => c.Column.BoardId == boardId &&
                            c.Id != request.CardId &&
                            !linkedCardIds.Contains(c.Id));

            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                var term = request.Query.Trim().ToLower();
                query = query.Where(c => c.Title.ToLower().Contains(term));
            }

            var candidates = await query
                .OrderByDescending(c => c.CreatedAt)
                .Take(20)
                .Select(c => new CardCandidateDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    ColumnTitle = c.Column != null ? c.Column.Title : null,
                    IsDone = (c.Column != null && c.Column.IsDoneColumn) || c.ProgressPercentage == 100,
                    Type = c.Type,
                    Priority = c.Priority,
                    AssigneeName = c.Assignee != null ? c.Assignee.Username : null
                })
                .ToListAsync(cancellationToken);

            return candidates;
        }
    }
}
