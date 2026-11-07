using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Enum;
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

            var sprints = await _context.Sprints
                .AsNoTracking()
                .Where(s => s.ProjectId == request.ProjectId)
                .Include(s => s.Cards)
                    .ThenInclude(c => c.Column)
                .OrderBy(s => s.Status == SprintStatus.Active ? 0 : s.Status == SprintStatus.Future ? 1 : 2)
                .ThenByDescending(s => s.CreatedAt)
                .ToListAsync(cancellationToken);

            return sprints.Select(s => new SprintDto
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
                CompletedStoryPoints = s.Cards.Where(c => c.Column != null && c.Column.IsDoneColumn).Sum(c => c.StoryPoints ?? 0)
            }).ToList();
        }
    }
}
