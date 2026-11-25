using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.ReorderColumns
{
    public class ReorderColumnsCommand : IRequest<bool>
    {
        public Guid BoardId { get; set; }
        public List<Guid> OrderedColumnIds { get; set; } = new();
    }
}
