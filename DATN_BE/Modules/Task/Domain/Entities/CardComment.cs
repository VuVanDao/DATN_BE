using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Shared.Domain;

namespace DATN_BE.Modules.Task.Domain.Entities
{
    public class CardComment : BaseEntity, ISoftDelete
    {
        public string ContentText { get; set; } = string.Empty;// nội dung comment
        // Thuộc Card nào
        public Guid CardId { get; set; }
        public Card Card { get; set; } = null!;
        // Người viết comment
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTimeOffset? DeletedAt { get; set; }
    }
}
