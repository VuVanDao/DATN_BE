using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteColumn;

public class DeleteColumnCommand : IRequest<bool>
{
    public Guid BoardId { get; set; }
    public Guid ColumnId { get; set; }
}
