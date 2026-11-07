using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectBacklog
{
    public class GetProjectBacklogQuery : IRequest<ProjectBacklogDto>
    {
        public Guid ProjectId { get; set; }

        public GetProjectBacklogQuery(Guid projectId)
        {
            ProjectId = projectId;
        }
    }
}
