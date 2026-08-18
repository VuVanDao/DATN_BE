using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Notifications.Domain.Enum;
using DATN_BE.Shared.Domain;
using DATN_BE.Shared.Enum;

namespace DATN_BE.Modules.Notifications.Domain.Entities
{
    public class Notification : BaseEntity
    {
        // Người nhận thông báo
        public Guid ReceiverId { get; set; }
        public User Receiver { get; set; } = null!;

        // Người tạo hành động (NULL nếu là thông báo hệ thống/AI)
        public Guid? SenderId { get; set; }
        public User? Sender { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public NotificationType Type { get; set; } = NotificationType.SystemAlert;

        // Thực thể liên quan để điều hướng bấm vào thông báo
        public EntityType? EntityType { get; set; } // Workspace, Project, Board, Card
        public Guid? EntityId { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTimeOffset? ReadAt { get; set; }
    }
}
