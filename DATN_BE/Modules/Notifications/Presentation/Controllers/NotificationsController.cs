using DATN_BE.Modules.Notifications.Application.Commands.MarkAllNotificationsAsRead;
using DATN_BE.Modules.Notifications.Application.Commands.MarkNotificationAsRead;
using DATN_BE.Modules.Notifications.Application.DTOs;
using DATN_BE.Modules.Notifications.Application.Queries.GetNotifications;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DATN_BE.Modules.Notifications.Presentation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly ISender _sender;

        public NotificationsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Lấy danh sách thông báo của người dùng hiện tại kèm số lượng chưa đọc
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<NotificationListResponseDto>>> GetNotifications(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool unreadOnly = false,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(
                new GetNotificationsQuery(pageIndex, pageSize, unreadOnly),
                cancellationToken);

            return Ok(ApiResponse<NotificationListResponseDto>.Ok(result, "Lấy danh sách thông báo thành công."));
        }

        /// <summary>
        /// Đánh dấu một thông báo là đã đọc
        /// </summary>
        [HttpPatch("{id:guid}/read")]
        public async Task<ActionResult<ApiResponse<NotificationDto>>> MarkAsRead(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new MarkNotificationAsReadCommand(id), cancellationToken);
            return Ok(ApiResponse<NotificationDto>.Ok(result, "Đã đánh dấu thông báo là đã đọc."));
        }

        /// <summary>
        /// Đánh dấu tất cả thông báo chưa đọc của người dùng hiện tại là đã đọc
        /// </summary>
        [HttpPatch("read-all")]
        public async Task<ActionResult<ApiResponse<int>>> MarkAllAsRead(
            CancellationToken cancellationToken = default)
        {
            var count = await _sender.Send(new MarkAllNotificationsAsReadCommand(), cancellationToken);
            return Ok(ApiResponse<int>.Ok(count, "Đã đánh dấu tất cả thông báo là đã đọc."));
        }
    }
}
