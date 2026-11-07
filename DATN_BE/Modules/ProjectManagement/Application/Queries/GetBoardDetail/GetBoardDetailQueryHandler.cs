using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetBoardDetail
{
    public class GetBoardDetailQueryHandler : IRequestHandler<GetBoardDetailQuery, BoardDetailDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly ICurrentUserService? _currentUserService;

        public GetBoardDetailQueryHandler(
            IApplicationDbContext context,
            IProjectAuthorizationService authorizationService,
            ICurrentUserService? currentUserService = null)
        {
            _context = context;
            _authorizationService = authorizationService;
            _currentUserService = currentUserService;
        }

        public async Task<BoardDetailDto> Handle(GetBoardDetailQuery request, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureCanViewProjectAsync(request.ProjectId, cancellationToken);

            var query = _context.Boards.AsNoTracking()
                .Include(b => b.Project)
                .Include(b => b.Columns)
                    .ThenInclude(c => c.Cards)
                        .ThenInclude(card => card.Assignee)
                .Include(b => b.Columns)
                    .ThenInclude(c => c.Cards)
                        .ThenInclude(card => card.Reporter)
                .Include(b => b.Columns)
                    .ThenInclude(c => c.Cards)
                        .ThenInclude(card => card.Comments);

            var board = await query.FirstOrDefaultAsync(
                b => b.Slug == request.BoardSlug && b.ProjectId == request.ProjectId,
                cancellationToken)
                ?? throw new NotFoundException("Board", request.BoardSlug);

            var currentUserId = _currentUserService?.UserId;
            var isPinned = false;

            if (currentUserId.HasValue)
            {
                isPinned = await _context.UserBoardPreferences
                    .AsNoTracking()
                    .Where(p => p.BoardId == board.Id && p.UserId == currentUserId.Value)
                    .Select(p => p.IsPinned)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            SprintDto? activeSprintDto = null;
            Guid? targetSprintId = request.SprintId;

            if (board.Type == BoardType.Scrum)
            {
                // Nếu chưa chỉ định sprintId, tìm Sprint đang Active của Dự án
                Sprint? activeSprint = null;
                if (targetSprintId.HasValue)
                {
                    activeSprint = await _context.Sprints
                        .AsNoTracking()
                        .Include(s => s.Cards)
                            .ThenInclude(c => c.Column)
                        .FirstOrDefaultAsync(s => s.Id == targetSprintId.Value && s.ProjectId == board.ProjectId, cancellationToken);
                }
                else
                {
                    activeSprint = await _context.Sprints
                        .AsNoTracking()
                        .Include(s => s.Cards)
                            .ThenInclude(c => c.Column)
                        .FirstOrDefaultAsync(s => s.ProjectId == board.ProjectId && s.Status == SprintStatus.Active, cancellationToken);

                    targetSprintId = activeSprint?.Id;
                }

                if (activeSprint != null)
                {
                    activeSprintDto = new SprintDto
                    {
                        Id = activeSprint.Id,
                        ProjectId = activeSprint.ProjectId,
                        Name = activeSprint.Name,
                        Goal = activeSprint.Goal,
                        StartDate = activeSprint.StartDate,
                        EndDate = activeSprint.EndDate,
                        Status = activeSprint.Status,
                        CreatedAt = activeSprint.CreatedAt,
                        TotalCards = activeSprint.Cards.Count,
                        CompletedCards = activeSprint.Cards.Count(c => c.Column != null && c.Column.IsDoneColumn),
                        TotalStoryPoints = activeSprint.Cards.Sum(c => c.StoryPoints ?? 0),
                        CompletedStoryPoints = activeSprint.Cards.Where(c => c.Column != null && c.Column.IsDoneColumn).Sum(c => c.StoryPoints ?? 0)
                    };
                }
            }

            return new BoardDetailDto
            {
                Id = board.Id,
                Title = board.Title,
                Slug = board.Slug,
                Type = board.Type.ToString(),
                ProjectId = board.ProjectId,
                ProjectName = board.Project?.Name ?? string.Empty,
                ProjectKey = board.Project?.ProjectKey,
                WorkspaceId = board.Project?.WorkspaceId,
                IsPinned = isPinned,
                ActiveSprint = activeSprintDto,
                Columns = board.Columns
                    .OrderBy(c => c.Position)
                    .Select(c => new ColumnDto
                    {
                        Id = c.Id,
                        Title = c.Title,
                        Position = c.Position,
                        IsDoneColumn = c.IsDoneColumn,
                        BoardId = c.BoardId,
                        Cards = c.Cards
                            .Where(card => board.Type != BoardType.Scrum || (targetSprintId.HasValue && card.SprintId == targetSprintId.Value))
                            .OrderBy(card => card.Position)
                            .Select(card => new CardDto
                            {
                                Id = card.Id,
                                Title = card.Title,
                                Description = card.Description,
                                Position = card.Position,
                                CoverUrl = card.CoverUrl,
                                Type = card.Type,
                                Priority = card.Priority,
                                StoryPoints = card.StoryPoints,
                                StartDate = card.StartDate,
                                DueDate = card.DueDate,
                                ProgressPercentage = card.ProgressPercentage,
                                ColumnId = card.ColumnId,
                                AssigneeId = card.AssigneeId,
                                AssigneeName = card.Assignee != null ? card.Assignee.Username : null,
                                AssigneeAvatarUrl = card.Assignee?.AvatarUrl,
                                ReporterId = card.ReporterId,
                                ReporterName = card.Reporter != null ? card.Reporter.Username : string.Empty,
                                ReporterAvatarUrl = card.Reporter?.AvatarUrl,
                                SprintId = card.SprintId,
                                CreatedAt = card.CreatedAt,
                                CommentsCount = card.Comments.Count
                            }).ToList()
                    }).ToList()
            };
        }
    }
}
