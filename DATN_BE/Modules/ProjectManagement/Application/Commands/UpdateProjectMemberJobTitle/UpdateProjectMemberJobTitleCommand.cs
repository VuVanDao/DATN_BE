using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateProjectMemberJobTitle
{
    public class UpdateProjectMemberJobTitleCommand : IRequest<ProjectMemberDto>
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public string? JobTitle { get; set; }
    }
}
