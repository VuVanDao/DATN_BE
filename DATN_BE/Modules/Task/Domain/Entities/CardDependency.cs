using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Shared.Domain;

namespace DATN_BE.Modules.Task.Domain.Entities
{
    public class CardDependency : BaseEntity
    {
        // Thẻ bị chặn (Blocked Card)
        public Guid BlockedCardId { get; set; }
        public Card BlockedCard { get; set; } = null!;
        // Thẻ gây chặn (Blocking Card - cần làm xong trước)
        public Guid BlockingCardId { get; set; }
        public Card BlockingCard { get; set; } = null!;
    }
}
