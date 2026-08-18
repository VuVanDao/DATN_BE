using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Shared.Domain;

namespace DATN_BE.Modules.Workspaces.Domain.Entities
{
    public class WorkspaceMember
    {
        public Guid WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
