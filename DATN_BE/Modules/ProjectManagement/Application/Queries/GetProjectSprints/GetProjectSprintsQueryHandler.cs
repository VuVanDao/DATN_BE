using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectSprints
{
    public class GetProjectSprintsQueryHandler : IRequestHandler<GetProjectSprintsQuery, List<SprintDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;

        public GetProjectSprintsQueryHandler(
            IApplicationDbContext context,
            IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }

        public async Task<List<SprintDto>> Handle(GetProjectSprintsQuery request, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureCanViewProjectAsync(request.ProjectId, cancellationToken);

            // Xác định Bảng Scrum mục tiêu
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
                return new List<SprintDto>();
            }

            var query = _context.Sprints
                .AsNoTracking()
                .Where(s => s.ProjectId == request.ProjectId && s.BoardId == targetBoardId.Value);

            if (!request.IncludeClosed)
            {
                query = query.Where(s => s.Status != SprintStatus.Closed);
            }

            var sprints = await query
                .Include(s => s.Cards)
                    .ThenInclude(c => c.Assignee)
                .Include(s => s.Cards)
                    .ThenInclude(c => c.Reporter)
                .Include(s => s.Cards)
                    .ThenInclude(c => c.Comments)
                .Include(s => s.Cards)
                    .ThenInclude(c => c.Column)
                .Include(s => s.Cards)
                    .ThenInclude(c => c.BlockedByDependencies)
                        .ThenInclude(dep => dep.BlockingCard)
                            .ThenInclude(bc => bc.Column)
                .OrderBy(s => s.Status == SprintStatus.Active ? 0 : s.Status == SprintStatus.Future ? 1 : 2)
                .ThenBy(s => s.CreatedAt)
                .ToListAsync(cancellationToken);

            return sprints.Select(s =>
            {
                var cardDtos = s.Cards
                    .OrderBy(c => c.Position)
                    .Select(c => new CardDto
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
                        IsCompleted = c.IsCompleted,
                        ColumnId = c.ColumnId,
                        AssigneeId = c.AssigneeId,
                        AssigneeName = c.Assignee?.Username,
                        AssigneeAvatarUrl = c.Assignee?.AvatarUrl,
                        ReporterId = c.ReporterId,
                        ReporterName = c.Reporter?.Username ?? string.Empty,
                        ReporterAvatarUrl = c.Reporter?.AvatarUrl,
                        SprintId = c.SprintId,
                        CreatedAt = c.CreatedAt,
                        CommentsCount = c.Comments.Count,
                        IsBlocked = c.BlockedByDependencies.Any(d => d.BlockingCard != null && !d.BlockingCard.IsDeleted && !((d.BlockingCard.Column != null && d.BlockingCard.Column.IsDoneColumn) || d.BlockingCard.IsCompleted)),
                        BlockedByCount = c.BlockedByDependencies.Count(d => d.BlockingCard != null && !d.BlockingCard.IsDeleted && !((d.BlockingCard.Column != null && d.BlockingCard.Column.IsDoneColumn) || d.BlockingCard.IsCompleted))
                    }).ToList();

                return new SprintDto
                {
                    Id = s.Id,
                    ProjectId = s.ProjectId,
                    BoardId = s.BoardId,
                    Name = s.Name,
                    Goal = s.Goal,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Status = s.Status,
                    CreatedAt = s.CreatedAt,
                    TotalCards = s.Cards.Count,
                    CompletedCards = s.Cards.Count(c => (c.Column != null && c.Column.IsDoneColumn) || c.IsCompleted),
                    TotalStoryPoints = s.Cards.Sum(c => c.StoryPoints ?? 0),
                    CompletedStoryPoints = s.Cards.Where(c => (c.Column != null && c.Column.IsDoneColumn) || c.IsCompleted).Sum(c => c.StoryPoints ?? 0),
                    Cards = cardDtos
                };
            }).ToList();
        }
    }
}
