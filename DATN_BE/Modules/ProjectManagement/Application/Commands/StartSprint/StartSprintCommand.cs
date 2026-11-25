using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.StartSprint
{
    public class StartSprintCommand : IRequest<SprintDto>
    {
        public Guid SprintId { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string? Goal { get; set; }
    }
}
