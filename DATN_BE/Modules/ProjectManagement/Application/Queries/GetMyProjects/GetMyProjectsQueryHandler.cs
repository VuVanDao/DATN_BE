using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetMyProjects
{
    public sealed class GetMyProjectsQueryHandler
        : IRequestHandler<GetMyProjectsQuery, List<MyProjectsWorkspaceDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetMyProjectsQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<MyProjectsWorkspaceDto>> Handle(
            GetMyProjectsQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này.");

            var query = _context.Projects
                .AsNoTracking()
                .Where(project => project.ProjectMembers.Any(member => member.UserId == currentUserId));

            if (request.WorkspaceId.HasValue)
            {
                query = query.Where(project => project.WorkspaceId == request.WorkspaceId.Value);
            }

            var projects = await query
                .OrderBy(project => project.Workspace.Name)
                .ThenBy(project => project.WorkspaceId)
                .ThenByDescending(project => project.CreatedAt)
                .ThenBy(project => project.Id)
                .Select(project => new
                {
                    WorkspaceName = project.Workspace.Name,
                    WorkspaceSlug = project.Workspace.Slug,
                    Project = new ProjectDto
                    {
                        Id = project.Id,
                        Name = project.Name,
                        ProjectKey = project.ProjectKey,
                        Description = project.Description,
                        WorkspaceId = project.WorkspaceId,
                        CreatedById = project.CreatedById,
                        CreatedByName = _context.Users
                            .Where(user => user.Id == project.CreatedById)
                            .Select(user => user.Username)
                            .FirstOrDefault() ?? string.Empty,
                        BoardsCount = project.Boards.Count,
                        MembersCount = project.ProjectMembers.Count,
                        SprintCount = project.Sprints.Count(s => !s.IsDeleted && s.Status != SprintStatus.Closed),
                        CreatedAt = project.CreatedAt,
                        IsMember = true
                    }
                })
                .ToListAsync(cancellationToken);

            return projects
                .GroupBy(item => new { item.Project.WorkspaceId, item.WorkspaceName, item.WorkspaceSlug })
                .Select(group => new MyProjectsWorkspaceDto
                {
                    WorkspaceId = group.Key.WorkspaceId,
                    WorkspaceName = group.Key.WorkspaceName,
                    WorkspaceSlug = group.Key.WorkspaceSlug,
                    Projects = group.Select(item => item.Project).ToList()
                })
                .ToList();
        }
    }
}
