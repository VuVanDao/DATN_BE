using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Shared.Domain;

namespace DATN_BE.Modules.Workspaces.Domain.Entities
{
    public class Workspace : BaseEntity, ISoftDelete
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImgUrl {  get; set; }
        public Guid OwnerId { get; set; }
        public User Owner { get; set; } = null!;
        public bool IsDeleted { get; set; } = false;
        public DateTimeOffset? DeletedAt { get; set; }

        public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
