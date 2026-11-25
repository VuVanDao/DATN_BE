using DATN_BE.Modules.Identity.Domain.Entities;

namespace DATN_BE.Modules.ProjectManagement.Domain.Entities
{
    public class UserBoardPreference
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid BoardId { get; set; }
        public Board Board { get; set; } = null!;

        public bool IsPinned { get; set; } = false;
        public DateTimeOffset LastViewedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
