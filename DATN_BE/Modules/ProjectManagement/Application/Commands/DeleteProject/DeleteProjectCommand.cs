using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteProject
{
    public class DeleteProjectCommand : IRequest<bool>
    {
        public Guid ProjectId { get; set; }
    }
}
