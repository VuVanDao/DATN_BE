using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectRoles
{
    public class GetProjectRoleQueryHandler : IRequestHandler<GetProjectRoleQuery, List<ProjectRoleDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetProjectRoleQueryHandler(IApplicationDbContext context)
        {
            this._context = context;
        }
        public async Task<List<ProjectRoleDto>> Handle(GetProjectRoleQuery request, CancellationToken cancellationToken)
        {
            return await _context.Roles.Where(r => r.Scope == RoleScope.Project).Select(r => new ProjectRoleDto
            {
                Name = r.Name,
                Description = r.Description,
                Id = r.Id
            }).ToListAsync(cancellationToken);
        }
    }
}