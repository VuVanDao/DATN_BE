using DATN_BE.Modules.Task.Application.DTOs;

namespace DATN_BE.Modules.ProjectManagement.Application.DTOs
{
    public class ColumnDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Position { get; set; }
        public bool IsDoneColumn { get; set; }
        public Guid BoardId { get; set; }
        public List<CardDto> Cards { get; set; } = new();
    }
}
