using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Shared.Domain;

namespace DATN_BE.Modules.Identity.Domain.Entities
{
    public class Role : BaseEntity
    {
        public string Name { get; set; } = string.Empty; 
        public RoleScope Scope { get; set; } = RoleScope.Workspace; // Global, Workspace, Project
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; } = false;

        // Navigation Properties
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
