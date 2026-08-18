using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Shared.Domain;

namespace DATN_BE.Modules.ProjectManagement.Domain.Entities
{
    public class Column : BaseEntity, ISoftDelete
    {
        public string Title { get; set; } = string.Empty;
        public int Position { get; set; }
        public bool IsDoneColumn { get; set; } = false;
        public Guid BoardId { get; set; }
        public Board Board { get; set; } = null!;
        public bool IsDeleted { get; set; } = false;
        public DateTimeOffset? DeletedAt { get; set; }

        public ICollection<Card> Cards { get; set; }
    }
}
