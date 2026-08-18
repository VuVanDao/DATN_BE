using DATN_BE.Modules.Identity.Domain.Entities;

namespace DATN_BE.Modules.ProjectManagement.Domain.Entities
{
    public class ProjectMember
    {
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid? RoleId { get; set; }
        public Role? Role { get; set; }

        public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
