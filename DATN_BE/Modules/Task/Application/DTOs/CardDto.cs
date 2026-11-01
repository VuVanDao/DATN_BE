using DATN_BE.Modules.Task.Domain.Enum;

namespace DATN_BE.Modules.Task.Application.DTOs
{
    public class CardDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Position { get; set; }
        public string? CoverUrl { get; set; }
        public CardType Type { get; set; } = CardType.Task; // Task, Bug, Story, Epic
        public CardPriority Priority { get; set; } = CardPriority.High; // Low, Medium, High, Urgent
        public int? StoryPoints { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public int ProgressPercentage { get; set; }
        public Guid ColumnId { get; set; }
        public Guid? AssigneeId { get; set; }
        public string? AssigneeName { get; set; }
        public string? AssigneeAvatarUrl { get; set; }
        public Guid ReporterId { get; set; }
        public string ReporterName { get; set; } = string.Empty;
        public string? ReporterAvatarUrl { get; set; }
        public Guid? SprintId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public int CommentsCount { get; set; }
    }
}
