using DATN_BE.Modules.Task.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Task.Application.Queries.GetProjectBacklogCards
{
    public class GetProjectBacklogCardsQuery : IRequest<List<CardDto>>
    {
        public Guid ProjectId { get; set; }

        public GetProjectBacklogCardsQuery(Guid projectId)
        {
            ProjectId = projectId;
        }
    }
}
