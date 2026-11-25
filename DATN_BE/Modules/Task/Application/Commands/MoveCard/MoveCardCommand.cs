using MediatR;

namespace DATN_BE.Modules.Task.Application.Commands.MoveCard
{
    public class MoveCardCommand : IRequest<bool>
    {
        public Guid CardId { get; set; }
        public Guid TargetColumnId { get; set; }
        public int NewPosition { get; set; }
    }
}
