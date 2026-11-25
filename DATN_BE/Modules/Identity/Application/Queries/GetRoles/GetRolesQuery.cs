using DATN_BE.Modules.Identity.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Identity.Application.Queries.GetRoles
{
    public class GetRolesQuery : IRequest<List<RoleDto>>
    {
    }
}
