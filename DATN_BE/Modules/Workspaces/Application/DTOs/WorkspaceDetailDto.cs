namespace DATN_BE.Modules.Workspaces.Application.DTOs
{
    public class WorkspaceDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string? CurrentUserRole { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public List<WorkspaceMemberDto> Members { get; set; } = new();
        public List<WorkspaceProjectDto> Projects { get; set; } = new();
    }
    public class WorkspaceProjectDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProjectKey { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MembersCount { get; set; }
        public int TasksCount { get; set; }
        public List<ProjectMemberAvatarDto> Members { get; set; } = new();
        public DateTimeOffset CreatedAt { get; set; }
    }
    public class ProjectMemberAvatarDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}
