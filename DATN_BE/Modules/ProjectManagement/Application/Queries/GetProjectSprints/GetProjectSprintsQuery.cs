using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectSprints
{
    public class GetProjectSprintsQuery : IRequest<List<SprintDto>>
    {
        public Guid ProjectId { get; set; }
        public Guid? BoardId { get; set; }
        public bool IncludeClosed { get; set; }

        public GetProjectSprintsQuery(Guid projectId, Guid? boardId = null, bool includeClosed = false)
        {
            ProjectId = projectId;
            BoardId = boardId;
            IncludeClosed = includeClosed;
        }
    }
}
