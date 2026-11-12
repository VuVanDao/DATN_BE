using DATN_BE.Modules.Notifications.Application.DTOs;
using DATN_BE.Modules.Notifications.Application.Helpers;
using DATN_BE.Modules.Notifications.Presentation.Hubs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.Notifications.Application.Commands.MarkNotificationAsRead
{
    public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, NotificationDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHubContext<NotificationHub>? _hubContext;
        private readonly ILogger<MarkNotificationAsReadCommandHandler>? _logger;

        public MarkNotificationAsReadCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IHubContext<NotificationHub>? hubContext = null,
            ILogger<MarkNotificationAsReadCommandHandler>? logger = null)
        {
            _context = context;
            _currentUserService = currentUserService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<NotificationDto> Handle(
            MarkNotificationAsReadCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này.");

            var notification = await _context.Notifications
                .Include(n => n.Sender)
                .FirstOrDefaultAsync(n => n.Id == request.NotificationId, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy thông báo.");

            if (notification.ReceiverId != currentUserId)
            {
                throw new ForbiddenException("Bạn không có quyền đánh dấu thông báo này.");
            }

            var updated = false;
            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                updated = true;
            }

            var targetUrls = await NotificationMapper.ResolveTargetUrlsAsync(_context, [notification], cancellationToken);
            var dto = NotificationMapper.ToDto(notification, targetUrls);

            if (updated && _hubContext != null)
            {
                try
                {
                    await _hubContext.Clients
                        .Group(NotificationHub.UserGroupName(currentUserId))
                        .SendAsync(
                            NotificationHub.NotificationMarkedRead,
                            new { notificationId = dto.Id, readAt = dto.ReadAt },
                            CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Không thể đồng bộ trạng thái đã đọc thông báo {NotificationId}.", dto.Id);
                }
            }

            return dto;
        }
    }
}
