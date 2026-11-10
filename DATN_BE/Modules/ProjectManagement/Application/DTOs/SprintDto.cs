using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Modules.Task.Application.DTOs;

namespace DATN_BE.Modules.ProjectManagement.Application.DTOs
{
    public class SprintDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid BoardId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Goal { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public SprintStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public int TotalCards { get; set; }
        public int CompletedCards { get; set; }
        public int TotalStoryPoints { get; set; }
        public int CompletedStoryPoints { get; set; }

        public List<CardDto> Cards { get; set; } = new();
    }
}
