using DATN_BE.Modules.Notifications.Domain.Enum;
using DATN_BE.Shared.Enum;

namespace DATN_BE.Shared.Application.Interfaces
{
    public sealed record NotificationDispatchItem(
        Guid ReceiverId,
        Guid? SenderId,
        string? SenderName,
        string? SenderAvatarUrl,
        NotificationType Type,
        string Title,
        string Content,
        EntityType? EntityType = null,
        Guid? EntityId = null,
        string? TargetUrl = null);

    public interface INotificationService
    {
        Task CreateAndSendManyAsync(
            IEnumerable<NotificationDispatchItem> items,
            CancellationToken cancellationToken = default);
    }
}
