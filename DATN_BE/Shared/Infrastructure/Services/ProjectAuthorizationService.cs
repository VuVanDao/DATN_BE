using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
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
                "Bạn không thuộc Workspace của Project này.");
        }

        public Task EnsureCanModifyProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            var currentUserId = GetCurrentUserId();
            return EnsureAsync(
                _context.Projects.AnyAsync(project =>
                    project.Id == projectId &&
                    project.ProjectMembers.Any(member => member.UserId == currentUserId), cancellationToken),
                "Bạn chưa tham gia Project này nên không thể thay đổi dữ liệu.");
        }

        private Guid GetCurrentUserId()
        {
            return _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này.");
        }

        private static async Task EnsureAsync(Task<bool> accessCheck, string forbiddenMessage)
        {
            // accessCheck
            /*
            - view: workspaceOwner hay thanh vien workspace co the view
            - modify: chi member thuoc project can do it
            */
            var hasAccess = await accessCheck;
            if (!hasAccess)
            {
                throw new ForbiddenException(forbiddenMessage);
            }
        }
    }
}
