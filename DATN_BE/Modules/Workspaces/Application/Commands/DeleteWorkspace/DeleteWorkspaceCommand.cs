using MediatR;

namespace DATN_BE.Modules.Workspaces.Application.Commands.DeleteWorkspace
{
    public class DeleteWorkspaceCommand : IRequest<bool>
    {
        public Guid WorkspaceId { get; set; }

        public DeleteWorkspaceCommand(Guid workspaceId)
        {
            WorkspaceId = workspaceId;
        }
    }
}
