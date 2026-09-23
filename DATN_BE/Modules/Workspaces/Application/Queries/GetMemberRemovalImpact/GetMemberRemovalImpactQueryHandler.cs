using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DATN_BE.Modules.Workspaces.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Workspaces.Application.Queries.GetMemberRemovalImpact
{
    public class GetMemberRemovalImpactQueryHandler : IRequestHandler<GetMemberRemovalImpactQuery, MemberRemovalImpactDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetMemberRemovalImpactQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<MemberRemovalImpactDto> Handle(GetMemberRemovalImpactQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
               ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này");
            var workspace = await _context.Workspaces
               .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, cancellationToken)
               ?? throw new NotFoundException("Workspace này không tồn tại.");
            
            var isOwner = workspace.OwnerId == currentUserId;
            var isSelfLeave = request.MemberId == currentUserId;
            var isSystemAdmin = false;
            if (!isOwner && !isSelfLeave)
            {
                isSystemAdmin = await _context.Users
                    .AnyAsync(u => u.Id == currentUserId && !u.IsDeleted && u.Role.Name == "SystemAdmin", cancellationToken);
            }
            if (!isOwner && !isSelfLeave && !isSystemAdmin)
            {
                throw new ForbiddenException("Bạn không có quyền xem thông tin ảnh hưởng của thành viên này.");
            }
            var isExistingInWorkspace = await _context.WorkspaceMembers.AnyAsync(wm => wm.UserId == request.MemberId, cancellationToken);
            if (!isExistingInWorkspace)
            {
                throw new BadRequestException("Thành viên này không có trong workpsace");
            }
            var leaderProjects = await _context.ProjectMembers
                .Where(pm => pm.UserId == request.MemberId && pm.Role.Name == "ProjectLeader" && pm.Project.WorkspaceId == workspace.Id)
                .Select(pm => new LeaderProjectDto
                {
                    ProjectId = pm.ProjectId,
                    ProjectName = pm.Project.Name
                })
                .ToListAsync(cancellationToken);
            var assignedCardsCount = await _context.Cards
                .CountAsync(c => c.Column.Board.Project.WorkspaceId == request.WorkspaceId && c.AssigneeId == request.MemberId, cancellationToken);

            return new MemberRemovalImpactDto
            {
                MemberId = request.MemberId,
                IsProjectLeader = leaderProjects.Count > 0,
                AssignedCardsCount = assignedCardsCount,
                LeaderProjects = leaderProjects
            };
        }
    }
}