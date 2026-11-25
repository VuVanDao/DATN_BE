using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateBoard
{
    public class UpdateBoardCommand : IRequest<BoardDto>
    {
        public Guid BoardId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public BoardType Type { get; set; } = BoardType.Kanban;
    }
}
