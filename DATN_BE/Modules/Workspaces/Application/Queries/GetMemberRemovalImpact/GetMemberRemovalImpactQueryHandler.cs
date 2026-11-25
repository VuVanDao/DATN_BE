using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DATN_BE.Modules.Workspaces.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
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
               ?? throw new UnauthorizedException("Bạn cần đăng nhập để thực hiện hành động này");

            // tìm workspace mà:
            // w.OwnerId == currentUserId: case workspaceOwner xoá thành viên
            // w.Members.Any(m => m.UserId == currentUserId): case thành viên tự out, check thành viên có thuộc wokrspace đó không
            var workspace = await _context.Workspaces
                   .AsNoTracking()
                   .Where(w => w.Id == request.WorkspaceId
                            && (w.OwnerId == currentUserId || w.Members.Any(m => m.UserId == currentUserId)))
                   .Select(w => new { w.Id, w.OwnerId })
                   .FirstOrDefaultAsync(cancellationToken)
                   ?? throw new NotFoundException("Không tìm thấy Workspace hoặc bạn không có quyền truy cập.");

            var isOwner = workspace.OwnerId == currentUserId;
            var isSelfLeave = request.MemberId == currentUserId;

            if (!isOwner && !isSelfLeave)
            {
                throw new ForbiddenException("Bạn không có quyền xem thông tin ảnh hưởng của thành viên này.");
            }

            if (request.MemberId == workspace.OwnerId)
            {
                throw new BadRequestException("Người sở hữu Workspace không thể xoá chính bản thân ra khỏi danh sách thành viên");
            }

            var isMemberInWorkspace = await _context.WorkspaceMembers.AnyAsync(
                wm => wm.WorkspaceId == workspace.Id && wm.UserId == request.MemberId, cancellationToken);

            if (!isMemberInWorkspace)
            {
                throw new NotFoundException("Thành viên này không có trong workspace");
            }

            var leaderProjects = await _context.ProjectMembers
                .AsNoTracking()
                .Where(pm => pm.UserId == request.MemberId && pm.Role.Name == RoleConstants.ProjectLeader && pm.Project.WorkspaceId == workspace.Id)
                .Select(pm => new LeaderProjectDto
                {
                    ProjectId = pm.ProjectId,
                    ProjectName = pm.Project.Name
                })
                .ToListAsync(cancellationToken);
            var assignedCardsCount = await _context.Cards
                .CountAsync(c => c.Column.Board.Project.WorkspaceId == workspace.Id && c.AssigneeId == request.MemberId, cancellationToken);

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