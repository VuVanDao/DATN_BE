
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateProjectLeader
{
    public class UpdateProjectMemberRoleCommand : IRequest<ProjectMemberDto>
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public Guid NewRoleId { get; set; }
    }
}