using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Security.Domain.Enum;
using DATN_BE.Shared.Domain;

namespace DATN_BE.Modules.Security.Domain.Entities
{
    public class ActivityLog : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        // Đối tượng bị tác động (Card, Project, Workspace, Board, Sprint...)
        public ActivityEntityType EntityType { get; set; }

        public Guid EntityId { get; set; }

        // Loại hành động (Created, MovedToColumn, UpdatedDueDate, AssignedUser, Deleted...)
        public ActivityActionType ActionType { get; set; }

        // Dữ liệu chi tiết sự thay đổi dạng JSONB trong PostgreSQL
        public string? DetailsJson { get; set; }
    }
}
