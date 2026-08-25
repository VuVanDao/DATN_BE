namespace DATN_BE.Modules.Identity.Application.DTOs
{
    public class AdminUserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
