using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectBoards
{
    public class GetProjectBoardsQuery : IRequest<List<BoardDto>>
    {
        public Guid ProjectId { get; }

        public GetProjectBoardsQuery(Guid projectId)
        {
            ProjectId = projectId;
        }
    }
}
