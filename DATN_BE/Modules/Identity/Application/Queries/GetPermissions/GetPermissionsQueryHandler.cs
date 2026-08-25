using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Application.Queries.GetPermissions
{
    public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, List<PermissionDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetPermissionsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
        {
            var Permissions = await _context.Permissions
                .OrderBy(p => p.Module)
                 .Select(p => new PermissionDto
                 {
                     Id = p.Id,
                     Code = p.Code,
                     Name = p.Name,
                     Module = p.Module
                 })
                .ToListAsync(cancellationToken);
            return Permissions;
        }
    }
}
