using DATN_BE.Modules.Notifications.Domain.Entities;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.Security.Domain.Entities;
using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Modules.Workspaces.Domain.Entities;
using DATN_BE.Shared.Domain;

namespace DATN_BE.Modules.Identity.Domain.Entities
{
    public class User : BaseEntity, ISoftDelete
    {
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTimeOffset? DeletedAt { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<Workspace> OwnedWorkspaces { get; set; } = new List<Workspace>();
        public ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
        public ICollection<Project> OwnedProjects { get; set; } = new List<Project>();
        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
        public ICollection<UserBoardPreference> UserBoardPreferences { get; set; } = new List<UserBoardPreference>();
        public ICollection<Card> AssignedCards { get; set; } = new List<Card>();
        public ICollection<Card> ReportedCards { get; set; } = new List<Card>();
        public ICollection<CardComment> UserCardComments { get; set; } = new List<CardComment>();
        public ICollection<ActivityLog> UserActivityLogs { get; set; } = new List<ActivityLog>();
        public ICollection<Notification> ReceivedNotifications { get; set; } = new List<Notification>();
        public ICollection<Notification> SentNotifications { get; set; } = new List<Notification>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    }
}
