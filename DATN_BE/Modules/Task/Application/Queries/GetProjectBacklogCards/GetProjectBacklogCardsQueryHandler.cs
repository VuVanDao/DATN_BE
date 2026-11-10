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

            // Tự động gỡ SprintId khỏi các thẻ thuộc Bảng Kanban nếu có (bảo toàn nguyên tắc thẻ Kanban độc lập với Scrum)
            var misplacedKanbanCards = await _context.Cards
                .Include(c => c.Column).ThenInclude(col => col.Board)
                .Where(c => c.SprintId != null && c.Column.Board.ProjectId == request.ProjectId && c.Column.Board.Type != BoardType.Scrum)
                .ToListAsync(cancellationToken);

            if (misplacedKanbanCards.Any())
            {
                foreach (var card in misplacedKanbanCards)
                {
                    card.SprintId = null;
                }
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Chỉ lấy các thẻ thuộc Bảng Scrum và chưa gán vào Sprint (SprintId == null)
            var cards = await _context.Cards
                .AsNoTracking()
                .Where(c => c.Column.Board.ProjectId == request.ProjectId 
                         && c.Column.Board.Type == BoardType.Scrum 
                         && c.SprintId == null)
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
