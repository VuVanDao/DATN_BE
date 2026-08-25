using DATN_BE.Modules.Identity.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Identity.Application.Queries.GetPermissions
{
    public class GetPermissionsQuery : IRequest<List<PermissionDto>>
    {
    }
}
