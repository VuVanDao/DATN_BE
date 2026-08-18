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

        public ICollection<Workspace> OwnedWorkspaces { get; set; } 
        public ICollection<WorkspaceMember> WorkspaceMembers { get; set; } 
        public ICollection<Project> OwnedProjects { get; set; } 
        public ICollection<ProjectMember> ProjectMembers { get; set; } 
        public ICollection<Card> AssignedCards { get; set; } 
        public ICollection<Card> ReportedCards { get; set; } 
        public ICollection<CardComment> UserCardComments { get; set; } 
        public ICollection<ActivityLog> UserActivityLogs { get; set; }
    }
}
