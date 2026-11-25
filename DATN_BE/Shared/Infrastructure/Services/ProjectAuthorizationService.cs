using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Shared.Infrastructure.Services
{
    public sealed class ProjectAuthorizationService : IProjectAuthorizationService
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ProjectAuthorizationService(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public Task EnsureCanViewProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            var currentUserId = GetCurrentUserId();
            return EnsureAsync(
                _context.Projects.AnyAsync(project =>
                    project.Id == projectId &&
                    (project.Workspace.OwnerId == currentUserId ||
                     project.Workspace.Members.Any(member => member.UserId == currentUserId) ||
                     project.ProjectMembers.Any(member => member.UserId == currentUserId)), cancellationToken),
                "Bạn không phải thành viên của workspace của dự án này nên không thể xem thông tin này");
        }

        public Task EnsureCanModifyProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            var currentUserId = GetCurrentUserId();
            return EnsureAsync(
                _context.Projects.AnyAsync(project =>
                    project.Id == projectId &&
                    (project.Workspace.OwnerId == currentUserId ||
                     project.ProjectMembers.Any(member =>
                        member.UserId == currentUserId &&
                        (member.Role.Name == RoleConstants.ProjectLeader ||
                         member.Role.Name == RoleConstants.ProjectMember))), cancellationToken),
                "Bạn chưa tham gia dự án này hoặc không có quyền thay đổi dữ liệu.");
        }

        public Task EnsureProjectLeaderAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            var currentUserId = GetCurrentUserId();
            return EnsureAsync(
                _context.Projects.AnyAsync(project =>
                    project.Id == projectId &&
                    (project.Workspace.OwnerId == currentUserId ||
                     project.ProjectMembers.Any(member =>
                         member.UserId == currentUserId &&
                         member.Role.Scope == RoleScope.Project &&
                         member.Role.Name == RoleConstants.ProjectLeader)), cancellationToken),
                "Chỉ chủ sở hữu Workspace hoặc Trưởng dự án mới có quyền thực hiện thao tác này.");
        }

        public Task EnsureCanManageProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
            => EnsureCanManageProjectAsync(projectId, false, cancellationToken);

        public Task EnsureCanViewProjectAuditAsync(Guid projectId, CancellationToken cancellationToken = default)
            => EnsureCanManageProjectAsync(projectId, true, cancellationToken);

        private Task EnsureCanManageProjectAsync(Guid projectId, bool includeDeleted, CancellationToken cancellationToken)
        {
            var currentUserId = GetCurrentUserId();
            var projects = includeDeleted ? _context.Projects.IgnoreQueryFilters() : _context.Projects;
            return EnsureAsync(
                projects.AnyAsync(project =>
                    project.Id == projectId &&
                    (project.Workspace.OwnerId == currentUserId ||
                     project.ProjectMembers.Any(member =>
                         member.UserId == currentUserId &&
                         member.Role.Scope == RoleScope.Project &&
                         member.Role.Name == RoleConstants.ProjectLeader)), cancellationToken),
                "Chỉ chủ sở hữu Workspace hoặc Trưởng dự án mới được quản lý dự án.");
        }

        private Guid GetCurrentUserId()
        {
            return _currentUserService.UserId
                ?? throw new UnauthorizedException("Bạn cần đăng nhập để thực hiện hành động này.");
        }

        private static async Task EnsureAsync(Task<bool> accessCheck, string forbiddenMessage)
        {
            var hasAccess = await accessCheck;
            if (!hasAccess)
            {
                throw new ForbiddenException(forbiddenMessage);
            }
        }
    }
}
