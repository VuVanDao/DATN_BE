using DATN_BE.Modules.Workspaces.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Workspaces.Application.Commands.AddWorkspaceMember
{
    public class AddWorkspaceMemberCommand : IRequest<WorkspaceMemberDto>
    {
        public Guid WorkspaceId { get; set; }
        public string Email { get; set; } = string.Empty;
        public Guid? RoleId { get; set; } // Nếu null -> Mặc định là member
    }
}
