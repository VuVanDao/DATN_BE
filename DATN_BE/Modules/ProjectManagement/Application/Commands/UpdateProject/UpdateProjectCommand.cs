using System.Text.Json.Serialization;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateProject
{
    public class UpdateProjectCommand : IRequest<ProjectDto>
    {
        [JsonIgnore]
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProjectKey { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
