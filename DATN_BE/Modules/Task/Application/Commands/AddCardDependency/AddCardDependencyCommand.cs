using DATN_BE.Modules.Task.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Task.Application.Commands.AddCardDependency
{
    public enum DependencyDirection
    {
        BlockedBy, // Thẻ CardId bị chặn bởi TargetCardId (TargetCardId phải làm xong trước)
        Blocking   // Thẻ CardId đang chặn TargetCardId (CardId phải làm xong trước)
    }

    public class AddCardDependencyRequest
    {
        public Guid TargetCardId { get; set; }
        public DependencyDirection Direction { get; set; } = DependencyDirection.BlockedBy;
    }

    public class AddCardDependencyCommand : IRequest<CardDependencyItemDto>
    {
        public Guid CardId { get; set; }
        public Guid TargetCardId { get; set; }
        public DependencyDirection Direction { get; set; } = DependencyDirection.BlockedBy;
    }
}
