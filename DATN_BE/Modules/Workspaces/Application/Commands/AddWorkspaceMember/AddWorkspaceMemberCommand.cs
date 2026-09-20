using DATN_BE.Modules.Workspaces.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Workspaces.Application.Commands.AddWorkspaceMember
{
    /// <summary>
    /// Một mục trong danh sách thành viên cần thêm
    /// </summary>
    public class MemberEntry
    {
        public string Email { get; set; } = string.Empty;
        public Guid? RoleId { get; set; }
    }
    public class AddWorkspaceMemberCommand : IRequest<AddWorkspaceMembersResultDto>
    {
        
        public Guid WorkspaceId { get; set; }
        public List<MemberEntry> Members { get; set; } = new();
    }
}
