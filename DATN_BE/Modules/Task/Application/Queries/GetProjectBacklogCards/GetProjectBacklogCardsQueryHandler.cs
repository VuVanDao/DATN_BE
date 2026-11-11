using DATN_BE.Modules.ProjectManagement.Domain.Enum;
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

            // Xác định Bảng Scrum
            Guid? targetBoardId = request.BoardId;
            if (!targetBoardId.HasValue)
            {
                targetBoardId = await _context.Boards
                    .AsNoTracking()
                    .Where(b => b.ProjectId == request.ProjectId && b.Type == BoardType.Scrum)
                    .OrderBy(b => b.CreatedAt)
                    .Select(b => (Guid?)b.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (!targetBoardId.HasValue)
            {
                return new List<CardDto>();
            }

            // Chỉ lấy các thẻ thuộc Bảng Scrum mục tiêu và chưa gán vào Sprint (SprintId == null)
            var cards = await _context.Cards
                .AsNoTracking()
                .Where(c => c.Column.BoardId == targetBoardId.Value 
                         && c.SprintId == null)
                .Include(c => c.Assignee)
                .Include(c => c.Reporter)
                .Include(c => c.Comments)
                .Include(c => c.Column)
                .Include(c => c.BlockedByDependencies)
                    .ThenInclude(dep => dep.BlockingCard)
                        .ThenInclude(bc => bc.Column)
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
                CommentsCount = c.Comments.Count,
                IsBlocked = c.BlockedByDependencies.Any(d => d.BlockingCard != null && !d.BlockingCard.IsDeleted && !((d.BlockingCard.Column != null && d.BlockingCard.Column.IsDoneColumn) || d.BlockingCard.ProgressPercentage == 100)),
                BlockedByCount = c.BlockedByDependencies.Count(d => d.BlockingCard != null && !d.BlockingCard.IsDeleted && !((d.BlockingCard.Column != null && d.BlockingCard.Column.IsDoneColumn) || d.BlockingCard.ProgressPercentage == 100))
            }).ToList();
        }
    }
}
