namespace DATN_BE.Modules.ProjectManagement.Application.DTOs
{
    // trả về thông tin các thành viên trong dự án
    public class ProjectMemberDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public DateTimeOffset JoinedAt { get; set; }
    }
}
