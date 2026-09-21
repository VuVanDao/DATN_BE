using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetWorkspaceProjects
{
    public class GetWorkspaceProjectsQueryHandler : IRequestHandler<GetWorkspaceProjectsQuery, List<ProjectDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetWorkspaceProjectsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<ProjectDto>> Handle(GetWorkspaceProjectsQuery request, CancellationToken cancellationToken)
        {
            var workspaceProject = await _context.Projects.AsNoTracking()
                .Where(p => p.WorkspaceId == request.WorkspaceId)
                .Include(p => p.CreatedBy)
                .Include(p => p.Boards)
                .Include(p => p.ProjectMembers)
                .Include(p => p.Sprints)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
            return workspaceProject.Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                ProjectKey = p.ProjectKey,
                Description = p.Description,
                WorkspaceId = p.WorkspaceId,
                CreatedById = p.CreatedById,
                CreatedByName = p.CreatedBy?.Username ?? string.Empty,
                BoardsCount = p.Boards.Count,
                MembersCount = p.ProjectMembers.Count,
                SprintCOunt = p.Sprints.Count,
                CreatedAt = p.CreatedAt
            }).ToList();
        }
    }
}
