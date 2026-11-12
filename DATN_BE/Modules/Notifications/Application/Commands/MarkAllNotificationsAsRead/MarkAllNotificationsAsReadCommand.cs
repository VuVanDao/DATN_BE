using MediatR;

namespace DATN_BE.Modules.Notifications.Application.Commands.MarkAllNotificationsAsRead
{
    public sealed record MarkAllNotificationsAsReadCommand : IRequest<int>;
}
