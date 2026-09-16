using System.Text.Json.Serialization;
using MediatR;

namespace DATN_BE.Modules.Identity.Application.Commands.AssignUserRoles
{
    public class AssignUserRoleCommand : IRequest<bool>
    {
        [JsonIgnore]
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }
}
