using DATN_BE.Shared.Domain;

namespace DATN_BE.Modules.Identity.Domain.Entities
{
    public class Permission : BaseEntity
    {
        public string Code { get; set; } = string.Empty; // workspace:create, board:delete, card:edit...
        public string Name { get; set; } = string.Empty; // Tên hiển thị quyền tiếng Việt
        public string Module { get; set; } = string.Empty; // Workspace, Board, Card, Sprint, AI...

        // Navigation Properties
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
