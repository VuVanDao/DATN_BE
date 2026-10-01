using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Shared.Domain;

namespace DATN_BE.Modules.Identity.Domain.Job
{
    public class PasswordResetSendBytEmailJob:BaseEntity
    {
        public Guid PasswordResetTokenId { get; set; }
        public string ToEmail { get; set; }
        public StatusSendEmail Status { get; set; } = StatusSendEmail.Pending;
        public int Attempts { get; set; } = 0;
        public DateTimeOffset? NextAttemptAt { get; set; }
        public DateTimeOffset? SentAt { get; set; }
        public string LastError { get; set; } = string.Empty;
    }
}
