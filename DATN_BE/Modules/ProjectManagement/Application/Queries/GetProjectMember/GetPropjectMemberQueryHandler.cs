using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectMember
{
    public class GetPropjectMemberQueryHandler : IRequestHandler<GetProjectMemberQuery, List<ProjectMemberDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;
        public GetPropjectMemberQueryHandler(IApplicationDbContext context, IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }
        public async Task<List<ProjectMemberDto>> Handle(GetProjectMemberQuery request, CancellationToken cancellationToken)
        {
            // check project exist
            var currProject = await _context.Projects
                .Where(p => p.Id == request.projectId)
                .Select(p => new { p.Id })
                .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new NotFoundException("Dự án không tồn tại");

            await _authorizationService.EnsureCanViewProjectAsync(currProject.Id, cancellationToken);
            // get list project member
            return await _context.ProjectMembers
                .AsNoTracking()
                .Where(pm => pm.ProjectId == request.projectId)
                .OrderByDescending(pm => pm.Role.Name == "ProjectLeader")
                .ThenBy(pm => pm.JoinedAt)
                .Select(pm => new ProjectMemberDto
                {
                    UserId = pm.UserId,
                    Username = pm.User.Username,
                    Email = pm.User.Email,
                    AvatarUrl = pm.User.AvatarUrl,
                    RoleId = pm.RoleId,
                    RoleName = pm.Role.Name,
                    JoinedAt = pm.JoinedAt
                })
                .ToListAsync(cancellationToken);
        }
    }
}
