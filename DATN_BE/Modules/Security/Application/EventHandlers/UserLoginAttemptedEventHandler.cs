using DATN_BE.Modules.Security.Domain.Entities;
using DATN_BE.Shared.Application.Events;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;

namespace DATN_BE.Modules.Security.Application.EventHandlers
{
    public class UserLoginAttemptedEventHandler : INotificationHandler<UserLoginAttemptedEvent>
    {
        private readonly IApplicationDbContext _context;
        public UserLoginAttemptedEventHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async System.Threading.Tasks.Task Handle(UserLoginAttemptedEvent notification, CancellationToken cancellationToken)
        {
            var log = new LoginHistory
            {
                UserId = notification.UserId,
                EmailAttempted = notification.Email,
                IpAddress = notification.IpAddress,
                UserAgent = notification.UserAgent,
                IsSuccess = notification.IsSuccess,
                FailureReason = notification.FailureReason
            };
            _context.LoginHistories.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
