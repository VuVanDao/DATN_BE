using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Modules.Task.Domain.Entities;
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
            Guid? targetActiveSprintId = request.SprintId;
            List<Card>? sprintCards = null;

            // Tìm activeSprint (nếu có) chỉ khi board là Scrum
            if (board.Type == BoardType.Scrum)
            {
                // Nếu chưa chỉ định sprintId, tìm Sprint đang Active của chính Bảng Scrum này
                Sprint? activeSprint = null;
                if (targetActiveSprintId.HasValue)
                {
                    activeSprint = await _context.Sprints
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == targetActiveSprintId.Value && s.BoardId == board.Id, cancellationToken);
                }
                else
                {
                    activeSprint = await _context.Sprints
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.BoardId == board.Id && s.Status == SprintStatus.Active, cancellationToken);

                    targetActiveSprintId = activeSprint?.Id;
                }

                if (targetActiveSprintId.HasValue)
                {
                    sprintCards = await _context.Cards
                        .AsNoTracking()
                        .Include(c => c.Assignee)
                        .Include(c => c.Reporter)
                        .Include(c => c.Comments)
                        .Include(c => c.Column)
                        .Where(c => c.SprintId == targetActiveSprintId.Value && c.Column.BoardId == board.Id)
                        .OrderBy(c => c.Position)
                        .ToListAsync(cancellationToken);

                    if (activeSprint != null)
                    {
                        activeSprintDto = new SprintDto
                        {
                            Id = activeSprint.Id,
                            ProjectId = activeSprint.ProjectId,
                            BoardId = activeSprint.BoardId,
                            Name = activeSprint.Name,
                            Goal = activeSprint.Goal,
                            StartDate = activeSprint.StartDate,
                            EndDate = activeSprint.EndDate,
                            Status = activeSprint.Status,
                            CreatedAt = activeSprint.CreatedAt,
                            TotalCards = sprintCards.Count,
                            CompletedCards = sprintCards.Count(c => (c.Column != null && c.Column.IsDoneColumn) || c.ProgressPercentage == 100),
                            TotalStoryPoints = sprintCards.Sum(c => c.StoryPoints ?? 0),
                            CompletedStoryPoints = sprintCards.Where(c => (c.Column != null && c.Column.IsDoneColumn) || c.ProgressPercentage == 100).Sum(c => c.StoryPoints ?? 0)
                        };
                    }
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
                    .Select(c =>
                    {
                        IEnumerable<Card> columnCards;
                        if (board.Type == BoardType.Scrum)
                        {
                            columnCards = sprintCards != null
                                ? sprintCards.Where(card => card.ColumnId == c.Id).OrderBy(card => card.Position)
                                : Enumerable.Empty<DATN_BE.Modules.Task.Domain.Entities.Card>();
                        }
                        else
                        {
                            columnCards = c.Cards.OrderBy(card => card.Position);
                        }

                        return new ColumnDto
                        {
                            Id = c.Id,
                            Title = c.Title,
                            Position = c.Position,
                            IsDoneColumn = c.IsDoneColumn,
                            BoardId = c.BoardId,
                            Cards = columnCards.Select(card => new CardDto
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
                        };
                    }).ToList()
            };
        }
    }
}
