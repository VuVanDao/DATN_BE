using MediatR;

namespace DATN_BE.Shared.Application.Events
{
    public class UserLoginAttemptedEvent : INotification
    {
        public Guid? UserId { get; }
        public string Email { get; }
        public string? IpAddress { get; }
        public string? UserAgent { get; }
        public bool IsSuccess { get; }
        public string? FailureReason { get; }
        public UserLoginAttemptedEvent(
           Guid? userId,
           string email,
           string? ipAddress,
           string? userAgent,
           bool isSuccess,
           string? failureReason)
        {
            UserId = userId;
            Email = email;
            IpAddress = ipAddress;
            UserAgent = userAgent;
            IsSuccess = isSuccess;
            FailureReason = failureReason;
        }
    }
}
