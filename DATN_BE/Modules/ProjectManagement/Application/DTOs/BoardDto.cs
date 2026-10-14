namespace DATN_BE.Modules.ProjectManagement.Application.DTOs
{
    public class BoardDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
    }
}
