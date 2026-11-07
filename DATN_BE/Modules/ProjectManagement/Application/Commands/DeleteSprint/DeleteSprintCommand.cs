using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteSprint
{
    public class DeleteSprintCommand : IRequest<bool>
    {
        public Guid SprintId { get; set; }

        public DeleteSprintCommand(Guid sprintId)
        {
            SprintId = sprintId;
        }
    }
}
