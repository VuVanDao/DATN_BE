using DATN_BE.Modules.Workspaces.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Workspaces.Application.Queries.GetWorkspaceRoles
{
    public class GetWorkspaceRolesQuery : IRequest<List<RoleDto>>
    {
    }
}
