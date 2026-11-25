using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.CompleteSprint
{
    public class CompleteSprintCommand : IRequest<SprintDto>
    {
        public Guid SprintId { get; set; }
        public Guid? MoveUnfinishedToSprintId { get; set; }
    }
}
