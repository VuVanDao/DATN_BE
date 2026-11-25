using MediatR;

namespace DATN_BE.Modules.Task.Application.Commands.DeleteCard
{
    public class DeleteCardCommand : IRequest<bool>
    {
        public Guid CardId { get; set; }
        public DeleteCardCommand(Guid carId)
        {
            this.CardId = carId;
        }
    }
}
