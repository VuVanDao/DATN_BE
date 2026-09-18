using DATN_BE.Modules.Workspaces.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Workspaces.Application.Commands.CreateWorkspace
{
    public class CreateWorkspaceCommand : IRequest<WorkspaceDto>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
