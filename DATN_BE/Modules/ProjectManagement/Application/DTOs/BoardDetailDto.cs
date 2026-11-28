namespace DATN_BE.Modules.ProjectManagement.Application.DTOs
{
    public class BoardDetailDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? ProjectKey { get; set; }
        public Guid? WorkspaceId { get; set; }
        public bool IsPinned { get; set; }
        public bool IsWorkspaceOwner { get; set; }
        public string? CurrentUserRole { get; set; }
        public SprintDto? ActiveSprint { get; set; }
        public List<ColumnDto> Columns { get; set; } = new();
    }
}
