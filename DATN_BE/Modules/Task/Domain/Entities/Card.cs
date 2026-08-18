using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.Task.Domain.Enum;
using DATN_BE.Shared.Domain;

namespace DATN_BE.Modules.Task.Domain.Entities
{
    public class Card : BaseEntity, ISoftDelete
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Position { get; set; } // Thứ tự từ trên xuống trong Column
        public string? CoverUrl { get; set; }
        public CardType Type { get; set; } = CardType.Task; // Task, Bug, Story, Epic
        public CardPriority Priority { get; set; } = CardPriority.Low; // Low, Medium, High, Urgent
        public int? StoryPoints { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public int ProgressPercentage { get; set; } = 0; // 0 - 100%

        // Thuộc Column nào
        public Guid ColumnId { get; set; }
        public Column Column { get; set; } = null!;

        // Người chịu trách nhiệm làm task (Assignee)
        public Guid? AssigneeId { get; set; }
        public User? Assignee { get; set; }

        // Người tạo task (Reporter)
        public Guid ReporterId { get; set; }
        public User Reporter { get; set; } = null!;

        // Thuộc Sprint nào (nếu null -> nằm trong Backlog)
        public Guid? SprintId { get; set; }

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTimeOffset? DeletedAt { get; set; }

        // Navigation Properties
        public ICollection<CardComment> Comments { get; set; } = new List<CardComment>();
        public ICollection<CardDependency> BlockedByDependencies { get; set; } = new List<CardDependency>();
        public ICollection<CardDependency> BlockingDependencies { get; set; } = new List<CardDependency>();
    }
}
