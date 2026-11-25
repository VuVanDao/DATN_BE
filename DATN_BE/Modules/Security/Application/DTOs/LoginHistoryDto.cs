namespace DATN_BE.Modules.Security.Application.DTOs
{
    public class LoginHistoryDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string EmailAttempted { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool IsSuccess { get; set; }
        public string? FailureReason { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
