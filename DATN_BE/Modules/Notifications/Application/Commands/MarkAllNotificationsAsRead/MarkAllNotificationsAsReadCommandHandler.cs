using DATN_BE.Modules.Notifications.Presentation.Hubs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.Notifications.Application.Commands.MarkAllNotificationsAsRead
{
    public class MarkAllNotificationsAsReadCommandHandler : IRequestHandler<MarkAllNotificationsAsReadCommand, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHubContext<NotificationHub>? _hubContext;
        private readonly ILogger<MarkAllNotificationsAsReadCommandHandler>? _logger;

        public MarkAllNotificationsAsReadCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IHubContext<NotificationHub>? hubContext = null,
            ILogger<MarkAllNotificationsAsReadCommandHandler>? logger = null)
        {
            _context = context;
            _currentUserService = currentUserService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<int> Handle(
            MarkAllNotificationsAsReadCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new UnauthorizedException("Bạn cần đăng nhập để thực hiện hành động này.");

            var unreadNotifications = await _context.Notifications
                .Where(n => n.ReceiverId == currentUserId && !n.IsRead)
                .ToListAsync(cancellationToken);

            if (unreadNotifications.Count == 0)
            {
                return 0;
            }

            var readAt = DateTimeOffset.UtcNow;
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = readAt;
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (_hubContext != null)
            {
                try
                {
                    await _hubContext.Clients
                        .Group(NotificationHub.UserGroupName(currentUserId))
                        .SendAsync(
                            NotificationHub.NotificationsReadAll,
                            new { readAt },
                            CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Không thể đồng bộ trạng thái đã đọc tất cả thông báo cho user {UserId}.", currentUserId);
                }
            }

            return unreadNotifications.Count;
        }
    }
}
