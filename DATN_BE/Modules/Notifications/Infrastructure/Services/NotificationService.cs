using DATN_BE.Modules.Notifications.Application.Helpers;
using DATN_BE.Modules.Notifications.Domain.Entities;
using DATN_BE.Modules.Notifications.Presentation.Hubs;
using DATN_BE.Shared.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.Notifications.Infrastructure.Services
{
    /// <summary>
    /// Hai pha: lưu PostgreSQL trước, sau đó đẩy SignalR best-effort cho người nhận đang online.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly IApplicationDbContext _context;
        private readonly IHubContext<NotificationHub>? _hubContext;
        private readonly ILogger<NotificationService>? _logger;

        public NotificationService(
            IApplicationDbContext context,
            IHubContext<NotificationHub>? hubContext = null,
            ILogger<NotificationService>? logger = null)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async System.Threading.Tasks.Task CreateAndSendManyAsync(
            IEnumerable<NotificationDispatchItem> items,
            CancellationToken cancellationToken = default)
        {
            var pending = items
                .Where(i => i.ReceiverId != Guid.Empty && (!i.SenderId.HasValue || i.ReceiverId != i.SenderId.Value))
                .ToList();

            if (pending.Count == 0)
            {
                return;
            }

            var createdPairs = new List<(Notification Entity, NotificationDispatchItem Source)>(pending.Count);
            foreach (var item in pending)
            {
                var entity = new Notification
                {
                    ReceiverId = item.ReceiverId,
                    SenderId = item.SenderId,
                    Title = item.Title,
                    Content = item.Content,
                    Type = item.Type,
                    EntityType = item.EntityType,
                    EntityId = item.EntityId,
                    IsRead = false,
                    ReadAt = null
                };

                _context.Notifications.Add(entity);
                createdPairs.Add((entity, item));
            }

            // Pha 1: Bắt buộc lưu vào Database trước khi phát sự kiện real-time
            await _context.SaveChangesAsync(cancellationToken);

            if (_hubContext == null)
            {
                return;
            }

            // Lọc ra danh sách các thông báo đang bị THIẾU LINK
            var needingUrls = createdPairs
                .Where(p => string.IsNullOrWhiteSpace(p.Source.TargetUrl) && p.Entity.EntityType.HasValue && p.Entity.EntityId.HasValue)
                .Select(p => p.Entity)
                .ToList();

            var resolvedUrls = needingUrls.Count > 0
                ? await NotificationMapper.ResolveTargetUrlsAsync(_context, needingUrls, CancellationToken.None)
                : null;

            // Pha 2: Đẩy SignalR best-effort cho từng người nhận
            foreach (var (entity, source) in createdPairs)
            {
                var dto = NotificationMapper.ToDto(
                    entity,
                    resolvedUrls,
                    source.TargetUrl,
                    source.SenderName,
                    source.SenderAvatarUrl);

                try
                {
                    await _hubContext.Clients
                        .Group(NotificationHub.UserGroupName(entity.ReceiverId))
                        .SendAsync(NotificationHub.NotificationReceived, dto, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(
                        ex,
                        "Không thể đẩy thông báo real-time {NotificationId} cho user {ReceiverId}.",
                        entity.Id,
                        entity.ReceiverId);
                }
            }
        }
    }
}
