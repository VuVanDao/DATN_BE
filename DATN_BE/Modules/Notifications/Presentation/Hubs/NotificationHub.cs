using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DATN_BE.Modules.Notifications.Presentation.Hubs
{
    [Authorize]
    public sealed class NotificationHub : Hub
    {
        public const string Route = "/hubs/notifications";
        public const string NotificationReceived = "NotificationReceived";
        public const string NotificationMarkedRead = "NotificationMarkedRead";
        public const string NotificationsReadAll = "NotificationsReadAll";

        public static string UserGroupName(Guid userId) => $"user:{userId:N}";

        public override async System.Threading.Tasks.Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    UserGroupName(userId.Value),
                    Context.ConnectionAborted);
            }

            await base.OnConnectedAsync();
        }

        private Guid? GetCurrentUserId()
        {
            var rawUserId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? Context.User?.FindFirstValue("sub");

            return Guid.TryParse(rawUserId, out var userId) ? userId : null;
        }
    }
}
