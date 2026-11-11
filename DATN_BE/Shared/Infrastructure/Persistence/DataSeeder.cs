using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Shared.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Shared.Infrastructure.Persistence
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // 0. Tự động áp dụng Migration nếu database chưa cập nhật
            await context.Database.MigrateAsync();

            // 1. Seed Roles nếu chưa có (Duyệt từng Role)
            var defaultRoles = new List<Role>
            {
                new() { Name = RoleConstants.SystemAdmin, Scope = RoleScope.Global, Description = "Quản trị viên toàn hệ thống", IsSystemRole = true },
                new() { Name = RoleConstants.User, Scope = RoleScope.Global, Description = "Người dùng hệ thống", IsSystemRole = true },
                new() { Name = RoleConstants.WorkspaceOwner, Scope = RoleScope.Workspace, Description = "Chủ sở hữu Không gian làm việc", IsSystemRole = true },
                new() { Name = RoleConstants.WorkspaceMember, Scope = RoleScope.Workspace, Description = "Thành viên Không gian làm việc", IsSystemRole = true },
                new() { Name = RoleConstants.ProjectLeader, Scope = RoleScope.Project, Description = "Người dẫn dắt dự án", IsSystemRole = true },
                new() { Name = RoleConstants.ProjectMember, Scope = RoleScope.Project, Description = "Thành viên dự án", IsSystemRole = true }
            };

            foreach (var role in defaultRoles)
            {
                if (!await context.Roles.AnyAsync(r => r.Name == role.Name))
                {
                    await context.Roles.AddAsync(role);
                }
            }
            await context.SaveChangesAsync();

            // 2. Seed tài khoản Admin mặc định nếu chưa tồn tại
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == RoleConstants.SystemAdmin);
            if (adminRole != null)
            {
                var existingAdmin = await context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Email == "admin@gmail.com" || u.Username == "admin");

                if (existingAdmin == null)
                {
                    var systemAdminAccount = new User
                    {
                        Username = "admin",
                        Email = "admin@gmail.com",
                        IsActive = true,
                        AvatarUrl = "",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Dao11082004@"),
                        Bio = "System Administrator",
                        RoleId = adminRole.Id
                    };
                    await context.Users.AddAsync(systemAdminAccount);
                    await context.SaveChangesAsync();
                }
                else
                {
                    // Nếu tài khoản Admin đã có nhưng mật khẩu đang là plaintext chưa hash BCrypt
                    if (!existingAdmin.PasswordHash.StartsWith("$2"))
                    {
                        existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Dao11082004@");
                        existingAdmin.RoleId = adminRole.Id;
                        existingAdmin.IsActive = true;
                        await context.SaveChangesAsync();
                    }
                }
            }
        }
    }
}
