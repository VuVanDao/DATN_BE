using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Identity.Domain.Enum;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Shared.Infrastructure.Persistence
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // 1. Seed Roles nếu chưa có
            if (!await context.Roles.AnyAsync())
            {
                var roles = new List<Role>
            {
                new() { Name = "SystemAdmin", Scope = RoleScope.Global, Description = "Quản trị viên toàn hệ thống", IsSystemRole = true },
                new() { Name = "WorkspaceOwner", Scope = RoleScope.Workspace, Description = "Quản trị viên Không gian làm việc", IsSystemRole = true },
                new() { Name = "ProjectOwner", Scope = RoleScope.Project, Description = "Quản lý dự án", IsSystemRole = true },
                new() { Name = "Developer", Scope = RoleScope.Workspace, Description = "Thành viên phát triển", IsSystemRole = true },
                new() { Name = "Observer", Scope = RoleScope.Workspace, Description = "Người xem / Khách", IsSystemRole = true }
            };
                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }
            // 2. Seed Permissions cơ bản nếu chưa có
            if (!await context.Permissions.AnyAsync())
            {
                var permissions = new List<Permission>
            {
                new() { Code = "workspace:create", Name = "Tạo Workspace mới", Module = "Workspace" },
                new() { Code = "workspace:delete", Name = "Xóa Workspace", Module = "Workspace" },
                new() { Code = "board:create", Name = "Tạo Bảng mới", Module = "Board" },
                new() { Code = "card:create", Name = "Tạo Thẻ công việc", Module = "Card" },
                new() { Code = "card:edit", Name = "Chỉnh sửa Thẻ công việc", Module = "Card" },
                new() { Code = "sprint:manage", Name = "Quản lý Sprint", Module = "Sprint" }
            };
                await context.Permissions.AddRangeAsync(permissions);
                await context.SaveChangesAsync();
            }
        }
    }
}
