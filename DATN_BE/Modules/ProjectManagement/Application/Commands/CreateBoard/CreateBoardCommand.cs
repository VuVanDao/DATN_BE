using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.CreateBoard
{
    public class CreateBoardCommand : IRequest<BoardDto>
    {
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public BoardType Type { get; set; } = BoardType.Kanban;
    }
}
