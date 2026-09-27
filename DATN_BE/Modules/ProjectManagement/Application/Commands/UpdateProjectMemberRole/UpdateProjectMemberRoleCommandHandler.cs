using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateProjectLeader;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateProjectMemberRole
{
    public class UpdateProjectMemberRoleCommandHandler : IRequestHandler<UpdateProjectMemberRoleCommand, ProjectMemberDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public UpdateProjectMemberRoleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        public async Task<ProjectMemberDto> Handle(UpdateProjectMemberRoleCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này");
            var project = await _context.Projects
                .Include(p => p.Workspace)
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.Role)
                    .ThenInclude(p => p.Users)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId && !p.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Project không tồn tại");
            // Chỉ WorkspaceOwner hoặc ProjectLeader mới có quyền đổi vai trò
            var isWorkspaceOwner = project.Workspace.OwnerId == currentUserId;
            var isProjectLeader = project.ProjectMembers.Any(pm => pm.UserId == currentUserId && pm.Role.Name == "ProjectLeader");
            if (!isWorkspaceOwner && !isProjectLeader)
            {
                throw new ForbiddenException("Bạn không có quyền cập nhật vai trò thành viên trong dự án này");
            }
            // Tìm thành viên cần đổi vai trò
            var targetMember = project.ProjectMembers.FirstOrDefault(pm => pm.UserId == request.UserId)
                ?? throw new NotFoundException("Thành viên không thuộc dự án này");
            // Kiểm tra Role mới có thuộc scope Project không
            var newRole = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.NewRoleId && r.Scope == RoleScope.Project, cancellationToken)
                ?? throw new BadRequestException("Vai trò được chỉ định không hợp lệ cho phạm vi");

            if (newRole.Name == "ProjectLeader")
            {
                var defaultMemberRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "ProjectMember" && r.Scope == RoleScope.Project, cancellationToken)
                    ?? throw new NotFoundException("Không tìm thấy vai trò 'ProjectMember'");
                // make oleProjectLeader down to member
                foreach (var oldLeader in project.ProjectMembers.Where(pm => pm.Role.Name == "ProjectLeader" && pm.UserId != targetMember.UserId))
                {
                    oldLeader.RoleId = defaultMemberRole.Id;
                }
            }
            targetMember.RoleId = newRole.Id;
            await _context.SaveChangesAsync(cancellationToken);

            return new ProjectMemberDto
            {
                UserId = targetMember.UserId,
                Username = targetMember.User?.Username ?? "",
                Email = targetMember.User?.Email ?? "",
                AvatarUrl = targetMember.User?.AvatarUrl,
                RoleId = newRole.Id,
                RoleName = newRole.Name,
                JoinedAt = targetMember.JoinedAt
            };
        }
    }
}
