using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetBoardDetail
{
    public class GetBoardDetailQuery : IRequest<BoardDetailDto>
    {
        public string BoardSlug { get; set; }
        public Guid ProjectId { get; set; }
        public Guid? SprintId { get; set; }

        public GetBoardDetailQuery(string boardSlug, Guid projectId, Guid? sprintId = null)
        {
            BoardSlug = boardSlug;
            ProjectId = projectId;
            SprintId = sprintId;
        }
    }
}
