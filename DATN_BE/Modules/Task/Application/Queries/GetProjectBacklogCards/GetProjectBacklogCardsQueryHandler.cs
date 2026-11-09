using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Task.Application.Queries.GetProjectBacklogCards
{
    public class GetProjectBacklogCardsQueryHandler : IRequestHandler<GetProjectBacklogCardsQuery, List<CardDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;

        public GetProjectBacklogCardsQueryHandler(
            IApplicationDbContext context,
            IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }

        public async Task<List<CardDto>> Handle(GetProjectBacklogCardsQuery request, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureCanViewProjectAsync(request.ProjectId, cancellationToken);

            var cards = await _context.Cards
                .AsNoTracking()
                .Where(c => c.Column.Board.ProjectId == request.ProjectId && c.SprintId == null)
                .Include(c => c.Assignee)
                .Include(c => c.Reporter)
                .Include(c => c.Comments)
                .Include(c => c.Column)
                .OrderBy(c => c.Position)
                .ToListAsync(cancellationToken);

            return cards.Select(c => new CardDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                Position = c.Position,
                CoverUrl = c.CoverUrl,
                Type = c.Type,
                Priority = c.Priority,
                StoryPoints = c.StoryPoints,
                StartDate = c.StartDate,
                DueDate = c.DueDate,
                ProgressPercentage = c.ProgressPercentage,
                ColumnId = c.ColumnId,
                AssigneeId = c.AssigneeId,
                AssigneeName = c.Assignee != null ? c.Assignee.Username : null,
                AssigneeAvatarUrl = c.Assignee?.AvatarUrl,
                ReporterId = c.ReporterId,
                ReporterName = c.Reporter != null ? c.Reporter.Username : string.Empty,
                ReporterAvatarUrl = c.Reporter?.AvatarUrl,
                SprintId = c.SprintId,
                CreatedAt = c.CreatedAt,
                CommentsCount = c.Comments.Count
            }).ToList();
        }
    }
}
