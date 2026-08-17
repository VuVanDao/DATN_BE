using DATN_BE.Modules.Workspaces.Domain.Entities;
using DATN_BE.Shared.Domain;

namespace DATN_BE.Modules.Identity.Domain.Entities
{
    public class User:BaseEntity, ISoftDelete
    {
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? AvartarURL { get; set; }
        public string? Bio { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTimeOffset? DeletedAt { get; set; }
        public ICollection<Workspace> OwnedWorkspaces { get; set; }
        public ICollection<WorkspaceMember> WorkspaceMembers {  get; set; }
    }
}
