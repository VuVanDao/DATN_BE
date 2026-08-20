using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetBoardDetail
{
    public class GetBoardDetailQuery : IRequest<BoardDetailDto>
    {
        public Guid BoardId { get; set; }
        public GetBoardDetailQuery(Guid boardId)
        {
            BoardId = boardId;
        }
    }
}
