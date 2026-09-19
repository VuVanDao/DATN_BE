using DATN_BE.Modules.Workspaces.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Workspaces.Application.Commands.UpdateWorkspace
{
    public class UpdateWorkspaceCommand:IRequest<WorkspaceDetailDto>
    {
        public Guid WorkspaceId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public IFormFile? Logo { get; set; }
        public IFormFile? CoverImage { get; set; }
    }
}
