namespace DATN_BE.Modules.Workspaces.Application.DTOs
{
    public class WorkspaceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImgUrl { get; set; }
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public int MembersCount { get; set; }
        public int ProjectsCount { get; set; }
        public string? CurrentUserRole { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
