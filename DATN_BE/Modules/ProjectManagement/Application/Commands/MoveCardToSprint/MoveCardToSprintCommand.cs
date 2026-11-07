using DATN_BE.Modules.Task.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.MoveCardToSprint
{
    public class MoveCardToSprintCommand : IRequest<CardDto>
    {
        public Guid CardId { get; set; }
        public Guid? SprintId { get; set; }
    }
}
