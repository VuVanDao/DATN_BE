namespace DATN_BE.Modules.Notifications.Application.DTOs
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public Guid ReceiverId { get; set; }
        public Guid? SenderId { get; set; }
        public string? SenderName { get; set; }
        public string? SenderAvatarUrl { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }
        public string? TargetUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTimeOffset? ReadAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class NotificationListResponseDto
    {
        public int UnreadCount { get; set; }
        public int TotalCount { get; set; }
        public bool HasMore { get; set; }
        public List<NotificationDto> Items { get; set; } = new();
    }
}
