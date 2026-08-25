using DATN_BE.Shared.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DATN_BE.Shared.Infrastructure.Security
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public PermissionAuthorizationHandler(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? context.User.FindFirstValue("sub");
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return;
            }
            // Tạo scope để lấy DbContext (vì AuthorizationHandler thường là Singleton/Scoped)
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            // Lấy danh sách tất cả các Permission Code của User thông qua các Role
            var userPermissions = await dbContext.Users
                .Where(u => u.Id == userId)
                .SelectMany(u => u.UserRoles)
                .Select(u => u.Role)
                .SelectMany(r => r.RolePermissions)
                .Select(rp => rp.Permission.Code)
                .ToListAsync();
            // Nếu user có Role SystemAdmin hoặc có chứa đúng Permission yêu cầu
            if (userPermissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
            }
        }
    }
}
