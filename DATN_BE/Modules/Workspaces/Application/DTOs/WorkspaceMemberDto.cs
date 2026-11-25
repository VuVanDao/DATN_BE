namespace DATN_BE.Modules.Workspaces.Application.DTOs
{
    public class WorkspaceMemberDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTimeOffset JoinAt { get; set; }
    }
}
