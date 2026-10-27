namespace DATN_BE.Modules.ProjectManagement.Application.DTOs
{
    public class ProjectDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProjectKey { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid CreatedById { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public int BoardsCount { get; set; }
        public int MembersCount { get; set; }
        public int SprintCount {  get; set; }
        public int CardCount {  get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool IsMember { get; set; }
        public string? CurrentUserRole { get; set; }
    }
}
