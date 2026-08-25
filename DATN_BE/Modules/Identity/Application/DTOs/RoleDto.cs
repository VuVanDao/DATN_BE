namespace DATN_BE.Modules.Identity.Application.DTOs
{
    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Scope { get; set; } = string.Empty;
        public bool IsSystemRole { get; set; }
        public List<PermissionDto> Permissions { get; set; } = new();
    }
}
