using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.CreateProject
{
    public class CreateProjectCommand : IRequest<ProjectDto>
    {
        // OwnerId lấy từ token
        public Guid WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProjectKey { get; set; } = string.Empty; // VD: "DATN", "TRL"
        public string? Description { get; set; }
    }
}
