namespace DATN_BE.Modules.ProjectManagement.Application.DTOs
{
    public sealed class MyProjectsWorkspaceDto
    {
        public Guid WorkspaceId { get; set; }
        public string WorkspaceName { get; set; } = string.Empty;
        public string WorkspaceSlug { get; set; } = string.Empty;
        public List<ProjectDto> Projects { get; set; } = new();
    }
}
