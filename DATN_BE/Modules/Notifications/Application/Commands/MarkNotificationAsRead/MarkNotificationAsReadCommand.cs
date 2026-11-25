using DATN_BE.Modules.Notifications.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Notifications.Application.Commands.MarkNotificationAsRead
{
    public sealed record MarkNotificationAsReadCommand(Guid NotificationId) : IRequest<NotificationDto>;
}
