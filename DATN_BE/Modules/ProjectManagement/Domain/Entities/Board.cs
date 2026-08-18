using DATN_BE.Shared.Domain;

namespace DATN_BE.Modules.ProjectManagement.Domain.Entities
{
    public class Board : BaseEntity, ISoftDelete
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Type { get; set; } = "Kanban";
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;
        public bool IsDeleted { get; set; } = false;
        public DateTimeOffset? DeletedAt { get; set; }

        public ICollection<Column> Columns { get; set; } = new List<Column>();
    }
}
