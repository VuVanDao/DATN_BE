using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Modules.Workspaces.Application.DTOs;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Workspaces.Application.Queries.GetWorkspaceRoles
{
    public class GetWorkspaceRolesQueryHandler : IRequestHandler<GetWorkspaceRolesQuery, List<RoleDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetWorkspaceRolesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<RoleDto>> Handle(GetWorkspaceRolesQuery request, CancellationToken cancellationToken)
        {
            // Lấy các role có scope = Workspace, LOẠI TRỪ WorkspaceOwner
            return await _context.Roles
                .Where(r => r.Scope == RoleScope.Workspace && r.Name != "WorkspaceOwner")
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description
                })
                .ToListAsync(cancellationToken);
        }
    }
}
