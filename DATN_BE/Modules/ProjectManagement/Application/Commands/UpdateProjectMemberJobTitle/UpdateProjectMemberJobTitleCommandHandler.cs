using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateProjectMemberJobTitle
{
    public class UpdateProjectMemberJobTitleCommandHandler : IRequestHandler<UpdateProjectMemberJobTitleCommand, ProjectMemberDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateProjectMemberJobTitleCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ProjectMemberDto> Handle(
            UpdateProjectMemberJobTitleCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new UnauthorizedException("Bạn cần đăng nhập để thực hiện hành động này");

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

            // Chỉ có WorkspaceOwner hoặc ProjectLeader mới có quyền cập nhật chức danh thành viên
            if (project.WorkspaceOwnerId != currentUserId && !project.IsCurrentUserProjectLeader)
            {
                throw new ForbiddenException("Chỉ có Chủ sở hữu Workspace hoặc Trưởng dự án mới có quyền cập nhật chức danh thành viên");
            }

            // Tìm thành viên trong dự án
            var targetMember = await _context.ProjectMembers
                .Include(pm => pm.User)
                .Include(pm => pm.Role)
                .FirstOrDefaultAsync(pm =>
                    pm.ProjectId == request.ProjectId &&
                    pm.UserId == request.UserId,
                    cancellationToken)
                ?? throw new NotFoundException("Thành viên không thuộc dự án này");

            // Cập nhật JobTitle (cắt khoảng trắng, giới hạn tối đa 100 ký tự)
            var trimmedTitle = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim();
            if (trimmedTitle != null && trimmedTitle.Length > 100)
            {
                trimmedTitle = trimmedTitle.Substring(0, 100);
            }

            targetMember.JobTitle = trimmedTitle;
            await _context.SaveChangesAsync(cancellationToken);

            return new ProjectMemberDto
            {
                UserId = targetMember.UserId,
                Username = targetMember.User?.Username ?? string.Empty,
                Email = targetMember.User?.Email ?? string.Empty,
                AvatarUrl = targetMember.User?.AvatarUrl,
                RoleId = targetMember.RoleId,
                RoleName = targetMember.Role?.Name ?? string.Empty,
                JobTitle = targetMember.JobTitle,
                JoinedAt = targetMember.JoinedAt
            };
        }
    }
}
