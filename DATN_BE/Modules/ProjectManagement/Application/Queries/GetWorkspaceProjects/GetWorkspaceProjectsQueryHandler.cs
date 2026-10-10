using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetWorkspaceProjects
{
    public class GetWorkspaceProjectsQueryHandler : IRequestHandler<GetWorkspaceProjectsQuery, List<ProjectDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public GetWorkspaceProjectsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        public async Task<List<ProjectDto>> Handle(GetWorkspaceProjectsQuery request, CancellationToken cancellationToken)
        {
            var CurrUserId = _currentUserService.UserId ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này");

            var checkUserBelongToWorkspace = await _context.WorkspaceMembers
                    .AsNoTracking()
                    .Where(wm => wm.WorkspaceId == request.WorkspaceId)
                    .AnyAsync(wm => wm.UserId == CurrUserId, cancellationToken);
            if (!checkUserBelongToWorkspace)
            {
                throw new ForbiddenException("Bạn không phải thành viên của workspace này, bạn không có quyền truy cập");
            }

            var workspaceProject = await _context.Projects.AsNoTracking()
                .Where(p => p.WorkspaceId == request.WorkspaceId)
                .Include(p => p.CreatedBy)
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
                MembersCount = p.ProjectMembers.Count,
                SprintCount = p.Sprints.Count,
                CreatedAt = p.CreatedAt
            }).ToList();
        }
    }
}
