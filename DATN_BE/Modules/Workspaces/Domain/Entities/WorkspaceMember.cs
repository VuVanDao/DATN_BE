using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Shared.Domain;

namespace DATN_BE.Modules.Workspaces.Domain.Entities
{
    public class WorkspaceMember : BaseEntity
    {
        public Guid WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public Guid RoleId { get; set; }
        public DateTimeOffset JoinAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
