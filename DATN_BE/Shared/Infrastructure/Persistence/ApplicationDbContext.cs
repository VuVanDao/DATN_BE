using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Notifications.Domain.Entities;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.Security.Domain.Entities;
using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Modules.Workspaces.Domain.Entities;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DATN_BE.Shared.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<Workspace> Workspaces => Set<Workspace>();
        public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
        public DbSet<Board> Boards => Set<Board>();
        public DbSet<Column> Columns => Set<Column>();
        public DbSet<Card> Cards => Set<Card>();
        public DbSet<CardComment> CardComments => Set<CardComment>();
        public DbSet<CardDependency> CardDependencies => Set<CardDependency>();
        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
        public DbSet<Sprint> Sprints => Set<Sprint>();
        public DbSet<UserBoardPreference> UserBoardPreferences => Set<UserBoardPreference>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Tự động load tất cả Configuration Fluent API trong Assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Tự động áp dụng Global Query Filter cho Soft Delete (IsDeleted = false)
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var propertyMethodInfo = typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(bool));
                    var isDeletedProperty = Expression.Call(propertyMethodInfo, parameter, Expression.Constant("IsDeleted"));
                    var compareExpression = Expression.Equal(isDeletedProperty, Expression.Constant(false));
                    var lambda = Expression.Lambda(compareExpression, parameter);
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }
        }
        // 1. Ghi đè phương thức SaveChangesAsync (Bất đồng bộ)
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            HandleBeforeSaveChanges();
            return await base.SaveChangesAsync(cancellationToken);
        }
        // 2. Ghi đè phương thức SaveChanges (Đồng bộ)
        public override int SaveChanges()
        {
            HandleBeforeSaveChanges();
            return base.SaveChanges();
        }
        // 3. Hàm cốt lõi: Tự động can thiệp vào ChangeTracker
        private void HandleBeforeSaveChanges()
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var entry in ChangeTracker.Entries())
            {
                // A. Tự động cập nhật CreatedAt và UpdatedAt cho BaseEntity
                if (entry.Entity is BaseEntity baseEntity)
                {
                    if (entry.State == EntityState.Added)
                    {
                        baseEntity.CreatedAt = now;
                        baseEntity.UpdatedAt = now;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        baseEntity.UpdatedAt = now;
                    }
                }
                // B. Tự động chuyển đổi Xóa cứng thành Xóa mềm cho ISoftDelete
                if (entry.Entity is ISoftDelete softDeleteEntity && entry.State == EntityState.Deleted)
                {
                    // Chuyển trạng thái từ "Xóa khỏi DB" thành "Cập nhật dòng này"
                    entry.State = EntityState.Modified;
                    // Gán các cờ xóa mềm
                    softDeleteEntity.IsDeleted = true;
                    softDeleteEntity.DeletedAt = now;

                    // Nếu đối tượng đó cũng kế thừa BaseEntity, cập nhật luôn UpdatedAt
                    if (entry.Entity is BaseEntity entityWithBase)
                    {
                        entityWithBase.UpdatedAt = now;
                    }
                }
            }
        }
    }
}
