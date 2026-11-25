using DATN_BE.Modules.Notifications.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Notifications.Application.Queries.GetNotifications
{
    public sealed record GetNotificationsQuery(
        int PageIndex = 1,
        int PageSize = 20,
        bool UnreadOnly = false) : IRequest<NotificationListResponseDto>;
}
