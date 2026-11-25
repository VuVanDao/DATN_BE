using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.CreateColumn
{
    public class CreateColumnCommand : IRequest<ColumnDto>
    {
        public Guid BoardId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsDoneColumn { get; set; } = false;
    }
}
