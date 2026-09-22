using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectMember
{
    public class GetPropjectMemberQueryHandler : IRequestHandler<GetProjectMemberQuery, List<ProjectMemberDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currenUserService;
        public GetPropjectMemberQueryHandler(IApplicationDbContext context,ICurrentUserService service)
        {
            _context = context;
            _currenUserService = service;

        }
        public async Task<List<ProjectMemberDto>> Handle(GetProjectMemberQuery request, CancellationToken cancellationToken)
        {
            var currUserId = _currenUserService.UserId ?? throw new ForbiddenException("Bạn cần đăng nhập để xem thông tin");
            // check project exist
            var currProject = await _context.Projects
                .Where(p => p.Id == request.projectId)
                .Select(p => new { p.Id, p.WorkspaceId, OwnerId = p.Workspace.OwnerId })
                .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new NotFoundException("Dự án không tồn tại");

            // user must be workspaceOwner or workspaceMember to see project
            var hasAccess = currProject.OwnerId == currUserId || await _context.WorkspaceMembers.AnyAsync(wm => wm.UserId == currUserId, cancellationToken);
            if(hasAccess == false)
            {
                throw new ForbiddenException("Bạn không có quyền xem thành viên của dự án này");
            }
            // get list project member
            return await _context.ProjectMembers
                .AsNoTracking()
                .Where(pm => pm.ProjectId == request.projectId)
                .OrderByDescending(pm => pm.Role.Name == "ProjectLeader")
                .ThenBy(pm => pm.JoinedAt)
                .Select(pm => new ProjectMemberDto
                {
                    UserId = pm.UserId,
                    Username = pm.User.Username,
                    Email = pm.User.Email,
                    AvatarUrl = pm.User.AvatarUrl,
                    RoleId = pm.RoleId,
                    RoleName = pm.Role.Name,
                    JoinedAt = pm.JoinedAt
                })
                .ToListAsync(cancellationToken);
        }
    }
}