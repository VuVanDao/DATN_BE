
using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Identity.Domain.Enum;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Shared.Infrastructure.Persistence
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // 1. Seed Roles nếu chưa có (Duyệt từng Role)
            var defaultRoles = new List<Role>
            {
                new() { Name = "SystemAdmin", Scope = RoleScope.Global, Description = "Quản trị viên toàn hệ thống", IsSystemRole = true },
                new() { Name = "User", Scope = RoleScope.Global, Description = "Người dùng hệ thống", IsSystemRole = true },
                new() { Name = "WorkspaceOwner", Scope = RoleScope.Workspace, Description = "Quản trị viên Không gian làm việc", IsSystemRole = true },
                new() { Name = "ProjectOwner", Scope = RoleScope.Project, Description = "Quản lý dự án", IsSystemRole = true },
                new() { Name = "Developer", Scope = RoleScope.Workspace, Description = "Thành viên phát triển", IsSystemRole = true },
                new() { Name = "WorkspaceMember", Scope = RoleScope.Workspace, Description = "Người xem / Khách của workspace", IsSystemRole = true }
            };
            foreach (var role in defaultRoles)
            {
                if (!await context.Roles.AnyAsync(r => r.Name == role.Name))
                {
                    await context.Roles.AddAsync(role);
                }
            }
            await context.SaveChangesAsync();

            // 2. Seed Permissions (Duyệt từng Permission - Thêm quyền nào còn thiếu)
            var defaultPermissions = new List<Permission>
            {
                new() { Code = "user:read", Name = "Xem danh sách người dùng", Module = "Identity" },
                new() { Code = "user:manage", Name = "Quản lý người dùng & Khóa tài khoản", Module = "Identity" },
                new() { Code = "role:manage", Name = "Quản lý Vai trò và Phân quyền", Module = "Identity" },
                new() { Code = "workspace:create", Name = "Tạo Workspace mới", Module = "Workspace" },
                new() { Code = "workspace:manage", Name = "Quản lý Workspace", Module = "Workspace" },
                new() { Code = "workspace:delete", Name = "Xóa Workspace", Module = "Workspace" },
                new() { Code = "project:create", Name = "Tạo Dự án mới", Module = "Project" },
                new() { Code = "project:manage", Name = "Quản lý Dự án", Module = "Project" },
                new() { Code = "board:create", Name = "Tạo Bảng mới", Module = "Board" },
                new() { Code = "card:create", Name = "Tạo Thẻ công việc", Module = "Card" },
                new() { Code = "card:edit", Name = "Chỉnh sửa Thẻ công việc", Module = "Card" },
                new() { Code = "card:delete", Name = "Xóa Thẻ công việc", Module = "Card" },
                new() { Code = "sprint:manage", Name = "Quản lý Sprint", Module = "Sprint" }
            };
            foreach (var perm in defaultPermissions)
            {
                if (!await context.Permissions.AnyAsync(p => p.Code == perm.Code))
                {
                    await context.Permissions.AddAsync(perm);
                }
            }
            await context.SaveChangesAsync();

            // 3. Gán TOÀN BỘ Permission cho SystemAdmin Role (Chạy độc lập)
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "SystemAdmin");
            if (adminRole != null)
            {
                // Lấy tất cả các Permission hiện có trong hệ thống
                var allPermissions = await context.Permissions.ToListAsync();
                // Lấy danh sách PermissionId đã được gán cho SystemAdmin
                var existingRolePermIds = await context.RolePermissions
                    .Where(rp => rp.RoleId == adminRole.Id)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync();
                // Lọc ra các Permission mà SystemAdmin chưa có
                var newRolePerms = allPermissions
                    .Where(p => !existingRolePermIds.Contains(p.Id))
                    .Select(p => new RolePermission
                    {
                        RoleId = adminRole.Id,
                        PermissionId = p.Id
                    })
                    .ToList();
                if (newRolePerms.Any())
                {
                    await context.RolePermissions.AddRangeAsync(newRolePerms);
                    await context.SaveChangesAsync();
                }
            }

        }
    }
}
