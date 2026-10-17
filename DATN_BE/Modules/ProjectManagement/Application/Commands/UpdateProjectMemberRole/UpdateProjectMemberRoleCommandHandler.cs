using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateProjectLeader;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateProjectMemberRole
{
    public class UpdateProjectMemberRoleCommandHandler : IRequestHandler<UpdateProjectMemberRoleCommand, ProjectMemberDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public UpdateProjectMemberRoleCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ProjectMemberDto> Handle(
            UpdateProjectMemberRoleCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này");

            var project = await _context.Projects
                .Where(p => p.Id == request.ProjectId)
                .Select(p => new
                {
                    WorkspaceOwnerId = p.Workspace.OwnerId,
                    IsCurrentUserProjectLeader = p.ProjectMembers.Any(pm =>
                        pm.UserId == currentUserId &&
                        pm.Role.Name == RoleConstants.ProjectLeader)
                })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Project không tồn tại");

            // check quyền: chỉ có WorkspaceOwner hoặc ProjectLeader mới có quyền cập nhật vai trò thành viên
            if (project.WorkspaceOwnerId != currentUserId && !project.IsCurrentUserProjectLeader)
            {
                throw new ForbiddenException("Bạn không có quyền cập nhật vai trò thành viên trong dự án này");
            }
            
            // tìm và check member có thuộc project
            var targetMember = await _context.ProjectMembers
                .Include(pm => pm.User)
                .FirstOrDefaultAsync(pm =>
                    pm.ProjectId == request.ProjectId &&
                    pm.UserId == request.UserId,
                    cancellationToken)
                ?? throw new NotFoundException("Thành viên không thuộc dự án này");

            var projectRoles = await _context.Roles
                .Where(role =>
                    role.Scope == RoleScope.Project &&
                    (role.Id == request.NewRoleId || role.Name == RoleConstants.ProjectMember))
                .ToListAsync(cancellationToken);

            var newRole = projectRoles.FirstOrDefault(role => role.Id == request.NewRoleId)
                ?? throw new BadRequestException("Vai trò được chỉ định không hợp lệ cho phạm vi");

            if (newRole.Name == RoleConstants.ProjectLeader)
            {
                var memberRole = projectRoles.FirstOrDefault(role => role.Name == RoleConstants.ProjectMember)
                    ?? throw new NotFoundException("Không tìm thấy vai trò 'ProjectMember'");

                var oldLeaders = await _context.ProjectMembers
                    .Where(pm =>
                        pm.ProjectId == request.ProjectId &&
                        pm.RoleId == newRole.Id &&
                        pm.UserId != request.UserId)
                    .ToListAsync(cancellationToken);

                foreach (var oldLeader in oldLeaders)
                {
                    oldLeader.RoleId = memberRole.Id;
                }
            }

            targetMember.RoleId = newRole.Id;
            await _context.SaveChangesAsync(cancellationToken);

            return new ProjectMemberDto
            {
                UserId = targetMember.UserId,
                Username = targetMember.User?.Username ?? string.Empty,
                Email = targetMember.User?.Email ?? string.Empty,
                AvatarUrl = targetMember.User?.AvatarUrl,
                RoleId = newRole.Id,
                RoleName = newRole.Name,
                JoinedAt = targetMember.JoinedAt
            };
        }
    }
}
