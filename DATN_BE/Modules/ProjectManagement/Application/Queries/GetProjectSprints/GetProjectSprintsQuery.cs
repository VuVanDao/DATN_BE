using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectSprints
{
    public class GetProjectSprintsQuery : IRequest<List<SprintDto>>
    {
        public Guid ProjectId { get; set; }

        public GetProjectSprintsQuery(Guid projectId)
        {
            ProjectId = projectId;
        }
    }
}
