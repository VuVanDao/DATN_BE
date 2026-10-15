using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteBoard
{
    public class DeleteBoardCommand : IRequest<bool>
    {
        public Guid BoardId { get; set; }
    }
}
