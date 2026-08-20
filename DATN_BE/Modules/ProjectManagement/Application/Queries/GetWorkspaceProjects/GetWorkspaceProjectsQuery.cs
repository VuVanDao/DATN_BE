using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetWorkspaceProjects
{
    public class GetWorkspaceProjectsQuery : IRequest<List<ProjectDto>>
    {
        public Guid WorkspaceId { get; set; }
        public GetWorkspaceProjectsQuery(Guid workspaceId)
        {
            WorkspaceId = workspaceId;
        }
    }
}
