namespace DATN_BE.Modules.Identity.Application.DTOs
{
    public class AuthResponseDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Token { get; set; } = string.Empty;
        public Boolean IsActive { get; set; }
        public List<string> Roles { get; set; } = new();
        public string RefreshToken { get; set; } = string.Empty;
        public DateTimeOffset RefreshTokenExpiresAt { get; set; }
    }
}
