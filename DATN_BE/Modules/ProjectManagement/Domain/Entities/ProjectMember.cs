using DATN_BE.Modules.Identity.Domain.Entities;

namespace DATN_BE.Modules.ProjectManagement.Domain.Entities
{
    public class ProjectMember
    {
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;

        /// <summary>
        /// Chức danh / Vị trí chuyên môn trong phạm vi dự án (VD: Frontend Dev, Content Writer, Kỹ sư giám sát...)
        /// </summary>
        public string? JobTitle { get; set; }

        public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
