using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Shared.Domain;

namespace DATN_BE.Modules.Security.Domain.Entities
{
    public class LoginHistory : BaseEntity
    {
        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public string EmailAttempted { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool IsSuccess { get; set; }
        public string? FailureReason { get; set; }
    }
}
