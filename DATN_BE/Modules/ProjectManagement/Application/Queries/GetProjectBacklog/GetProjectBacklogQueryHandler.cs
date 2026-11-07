using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectBacklog
{
    public class GetProjectBacklogQueryHandler : IRequestHandler<GetProjectBacklogQuery, ProjectBacklogDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;

        public GetProjectBacklogQueryHandler(
            IApplicationDbContext context,
            IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }

        public async Task<ProjectBacklogDto> Handle(GetProjectBacklogQuery request, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureCanViewProjectAsync(request.ProjectId, cancellationToken);

            // 1. Lấy tất cả các Sprint chưa đóng (Active và Future)
            var sprints = await _context.Sprints
                .AsNoTracking()
                .Where(s => s.ProjectId == request.ProjectId && s.Status != SprintStatus.Closed)
                .Include(s => s.Cards)
                    .ThenInclude(c => c.Assignee)
                .Include(s => s.Cards)
                    .ThenInclude(c => c.Reporter)
                .Include(s => s.Cards)
                    .ThenInclude(c => c.Comments)
                .Include(s => s.Cards)
                    .ThenInclude(c => c.Column)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync(cancellationToken);

            // 2. Lấy các Cards thuộc Backlog (SprintId == null)
            var backlogCards = await _context.Cards
                .AsNoTracking()
                .Where(c => c.Column.Board.ProjectId == request.ProjectId && c.SprintId == null)
                .Include(c => c.Assignee)
                .Include(c => c.Reporter)
                .Include(c => c.Comments)
                .OrderBy(c => c.Position)
                .ToListAsync(cancellationToken);

            SprintDto MapSprintDto(Domain.Entities.Sprint s)
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
                        ProgressPercentage = c.ProgressPercentage,
                        ColumnId = c.ColumnId,
                        AssigneeId = c.AssigneeId,
                        AssigneeName = c.Assignee?.Username,
                        AssigneeAvatarUrl = c.Assignee?.AvatarUrl,
                        ReporterId = c.ReporterId,
                        ReporterName = c.Reporter?.Username ?? string.Empty,
                        ReporterAvatarUrl = c.Reporter?.AvatarUrl,
                        SprintId = c.SprintId,
                        CreatedAt = c.CreatedAt,
                        CommentsCount = c.Comments.Count
                    }).ToList();

                return new SprintDto
                {
                    Id = s.Id,
                    ProjectId = s.ProjectId,
                    Name = s.Name,
                    Goal = s.Goal,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Status = s.Status,
                    CreatedAt = s.CreatedAt,
                    TotalCards = s.Cards.Count,
                    CompletedCards = s.Cards.Count(c => c.Column != null && c.Column.IsDoneColumn),
                    TotalStoryPoints = s.Cards.Sum(c => c.StoryPoints ?? 0),
                    CompletedStoryPoints = s.Cards.Where(c => c.Column != null && c.Column.IsDoneColumn).Sum(c => c.StoryPoints ?? 0),
                    Cards = cardDtos
                };
            }

            var activeSprintEntity = sprints.FirstOrDefault(s => s.Status == SprintStatus.Active);
            var futureSprintEntities = sprints.Where(s => s.Status == SprintStatus.Future).ToList();

            var backlogCardDtos = backlogCards.Select(c => new CardDto
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
                AssigneeName = c.Assignee?.Username,
                AssigneeAvatarUrl = c.Assignee?.AvatarUrl,
                ReporterId = c.ReporterId,
                ReporterName = c.Reporter?.Username ?? string.Empty,
                ReporterAvatarUrl = c.Reporter?.AvatarUrl,
                SprintId = c.SprintId,
                CreatedAt = c.CreatedAt,
                CommentsCount = c.Comments.Count
            }).ToList();

            return new ProjectBacklogDto
            {
                ProjectId = request.ProjectId,
                ActiveSprint = activeSprintEntity != null ? MapSprintDto(activeSprintEntity) : null,
                FutureSprints = futureSprintEntities.Select(MapSprintDto).ToList(),
                BacklogCards = backlogCardDtos,
                BacklogTotalCards = backlogCardDtos.Count,
                BacklogTotalStoryPoints = backlogCardDtos.Sum(c => c.StoryPoints ?? 0)
            };
        }
    }
}
