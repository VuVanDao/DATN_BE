using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Identity.Domain.Job;
using DATN_BE.Modules.Notifications.Domain.Entities;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.Security.Domain.Entities;
using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Modules.Workspaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Shared.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Role> Roles { get; }
        DbSet<Workspace> Workspaces { get; }
        DbSet<WorkspaceMember> WorkspaceMembers { get; }
        DbSet<Project> Projects { get; }
        DbSet<ProjectMember> ProjectMembers { get; }
        DbSet<Board> Boards { get; }
        DbSet<Column> Columns { get; }
        DbSet<Card> Cards { get; }
        DbSet<CardComment> CardComments { get; }
        DbSet<CardDependency> CardDependencies { get; }
        DbSet<ActivityLog> ActivityLogs { get; }
        DbSet<Sprint> Sprints { get; }
        DbSet<UserBoardPreference> UserBoardPreferences { get; }
        DbSet<Notification> Notifications { get; }
        DbSet<RefreshToken> RefreshTokens { get; }
        DbSet<PasswordResetToken> PasswordResetTokens { get; }
        DbSet<LoginHistory> LoginHistories { get; }
        DbSet<PasswordResetSendBytEmailJob> PasswordResetEmailJob { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
