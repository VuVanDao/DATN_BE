using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Workspaces.Application.Commands.RemoveMemberFromProject
{
    public class RemoveMemberFromWorkspaceCommandHandler : IRequestHandler<RemoveMemberFromWorkspaceCommand, string>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public RemoveMemberFromWorkspaceCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        public async Task<string> Handle(RemoveMemberFromWorkspaceCommand request, CancellationToken cancellationToken)
        {
            // 1. Xác thực người dùng hiện tại
            var currentUserId = _currentUserService.UserId
                ?? throw new UnauthorizedException("Bạn cần đăng nhập để thực hiện hành động này");

            // 2. Tìm Workspace
            var workspace = await _context.Workspaces
                .Where(w => w.Id == request.WorkspaceId && (w.OwnerId == currentUserId || w.Members.Any(m => m.UserId == currentUserId)))
                .FirstOrDefaultAsync( cancellationToken)
                ?? throw new NotFoundException("Workspace này không tồn tại hoặc bạn không có quyền truy cập");

            var isOwner = workspace.OwnerId == currentUserId;
            var isSelfLeave = request.MemberId == currentUserId;

            if (!isOwner && !isSelfLeave )
            {
                throw new ForbiddenException("Bạn không có quyền xoá thành viên khỏi Workspace này");
            }

            // 4. Chặn xoá chính Chủ sở hữu Workspace
            if (request.MemberId == workspace.OwnerId)
            {
                throw new BadRequestException("Chủ sở hữu không thể bị xoá khỏi Workspace. Vui lòng chuyển giao quyền sở hữu hoặc xoá Workspace");
            }

            // 5. Thành viên phải đang tồn tại trong Workspace
            var targetMember = await _context.WorkspaceMembers
                .FirstOrDefaultAsync(wm => wm.WorkspaceId == request.WorkspaceId && wm.UserId == request.MemberId, cancellationToken)
                ?? throw new NotFoundException("Thành viên không tồn tại trong Workspace này");

            // 6. Chặn nếu thành viên đang là Trưởng dự án ở bất kỳ project nào trong Workspace
            // trả về tên các project
            var leaderProjectNames = await _context.ProjectMembers
                .Where(pm => pm.UserId == request.MemberId
                          && pm.Project.WorkspaceId == request.WorkspaceId
                          && pm.Role.Name == RoleConstants.ProjectLeader)
                .Select(pm => pm.Project.Name)
                .ToListAsync(cancellationToken);
            if (leaderProjectNames.Count > 0)
            {
                throw new BadRequestException(
                    $"Thành viên này đang là Trưởng dự án ở: {string.Join(", ", leaderProjectNames)}. " +
                    "Vui lòng chuyển giao vai trò Trưởng dự án trước khi xoá khỏi Workspace");
            }

            // 7. Gỡ thành viên khỏi mọi Project trong Workspace này
            var ListOfParticipatingMemberProjects = await _context.ProjectMembers
                .Where(pm => pm.UserId == request.MemberId && pm.Project.WorkspaceId == request.WorkspaceId)
                .ToListAsync(cancellationToken);
            _context.ProjectMembers.RemoveRange(ListOfParticipatingMemberProjects);

            // 8. Bỏ gán thành viên khỏi mọi Card thuộc Workspace này (CHỈ trong Workspace này)
            var cardsAssignedToMember = await _context.Cards
               .Where(c => c.Column.Board.Project.WorkspaceId == request.WorkspaceId && c.AssigneeId == request.MemberId)
               .ToListAsync(cancellationToken);

            foreach (var card in cardsAssignedToMember)
            {
                card.AssigneeId = null;
            }
            
            // 9. Xoá thành viên khỏi Workspace
            _context.WorkspaceMembers.Remove(targetMember);

            // 10. Một SaveChanges duy nhất => mọi thay đổi trên nằm trong cùng 1 transaction
            await _context.SaveChangesAsync(cancellationToken);

            return isOwner
                ? "Xoá thành viên khỏi Workspace thành công."
                : "Rời khỏi workspace thành công";
        }
    }
}