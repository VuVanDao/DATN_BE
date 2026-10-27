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
                     project.Workspace.Members.Any(member => member.UserId == currentUserId)), cancellationToken),
                "Bạn không phải thành viên của workspace của dự án này nên không thể xem thông tin này");
        }

        public Task EnsureCanModifyProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            var currentUserId = GetCurrentUserId();
            return EnsureAsync(
                _context.Projects.AnyAsync(project =>
                    project.Id == projectId &&
                    (project.Workspace.OwnerId == currentUserId ||
                     project.ProjectMembers.Any(member => member.UserId == currentUserId)), cancellationToken),
                "Bạn chưa tham gia dự án này nên không thể thay đổi dữ liệu.");
        }

        public Task EnsureCanManageProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            var currentUserId = GetCurrentUserId();
            return EnsureAsync(
                _context.Projects.AnyAsync(project =>
                    project.Id == projectId &&
                    (project.Workspace.OwnerId == currentUserId ||
                     project.ProjectMembers.Any(member =>
                         member.UserId == currentUserId &&
                         member.Role.Name == RoleConstants.ProjectLeader &&
                         member.Role.Scope == RoleScope.Project)), cancellationToken),
                "Chỉ chủ sở hữu Workspace hoặc Trưởng dự án mới được quản lý dự án.");
        }

        private Guid GetCurrentUserId()
        {
            return _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này.");
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
