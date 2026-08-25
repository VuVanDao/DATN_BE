using MediatR;

namespace DATN_BE.Modules.Identity.Application.Commands.AssignUserRoles
{
    public class AssignUserRolesCommand : IRequest<bool>
    {
        public Guid UserId { get; set; }
        public List<Guid> RoleIds { get; set; } = new List<Guid>();
    }
}
