using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Shared.Domain;

namespace DATN_BE.Modules.ProjectManagement.Domain.Entities
{
    public class Sprint : BaseEntity, ISoftDelete
    {
        public string Name { get; set; } = string.Empty;
        public string? Goal { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public SprintStatus Status { get; set; } = SprintStatus.Future;

        // Thuộc Project nào
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTimeOffset? DeletedAt { get; set; }

        // Navigation Properties
        public ICollection<Card> Cards { get; set; } = new List<Card>();
    }
}
