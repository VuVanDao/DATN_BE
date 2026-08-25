using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Application.Queries.GetRoles
{
    public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, List<RoleDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetRolesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {
            var Roles = await _context.Roles.Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Scope = r.Scope.ToString(),
                    IsSystemRole = r.IsSystemRole,
                    Permissions = r.RolePermissions.Select(rp => new PermissionDto
                    {
                        Id = rp.Permission.Id,
                        Code = rp.Permission.Code,
                        Name = rp.Permission.Name,
                        Module = rp.Permission.Module
                    }).ToList()
                })
                .ToListAsync(cancellationToken);
            return Roles;
        }
    }
}
