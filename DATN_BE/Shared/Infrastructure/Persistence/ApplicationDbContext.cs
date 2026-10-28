using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Identity.Domain.Job;
using DATN_BE.Modules.Notifications.Domain.Entities;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.Security.Domain.Entities;
using DATN_BE.Modules.Security.Infrastructure.Auditing;
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
        private readonly ICurrentUserService _currentUserService;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
            ICurrentUserService currentUserService) : base(options)
        {
            _currentUserService = currentUserService;
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
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
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
        public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
        public DbSet<PasswordResetSendBytEmailJob> PasswordResetEmailJob => Set<PasswordResetSendBytEmailJob>();

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
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => SaveChangesAsync(true, cancellationToken);

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            var auditLogs = PrepareChanges();
            try
            {
                // Business changes and audit rows commit in the same transaction.
                return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }
            catch
            {
                DetachAuditLogs(auditLogs);
                throw;
            }
        }

        public override int SaveChanges() => SaveChanges(true);

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var auditLogs = PrepareChanges();
            try
            {
                return base.SaveChanges(acceptAllChangesOnSuccess);
            }
            catch
            {
                DetachAuditLogs(auditLogs);
                throw;
            }
        }

        private List<ActivityLog> PrepareChanges()
        {
            ChangeTracker.DetectChanges();
            HandleBeforeSaveChanges();
            ChangeTracker.DetectChanges();

            // Seeder/background jobs have no authenticated actor; user commands use JWT identity.
            if (_currentUserService.UserId is not Guid actorId)
                return new List<ActivityLog>();

            var now = DateTimeOffset.UtcNow;
            var auditLogs = ChangeTracker.Entries().ToArray()
                .Select(entry => WorkspaceProjectAuditLogFactory.Create(entry, actorId, now))
                .OfType<ActivityLog>()
                .ToList();
            ActivityLogs.AddRange(auditLogs);
            return auditLogs;
        }

        private void DetachAuditLogs(IEnumerable<ActivityLog> auditLogs)
        {
            // A failed save must not leave audit rows queued for a later retry.
            foreach (var auditLog in auditLogs)
                Entry(auditLog).State = EntityState.Detached;
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
