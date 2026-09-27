using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.RemoveMemberFromProject
{
    public class RemoveMemberFormProjectCommandHandler : IRequestHandler<RemoveMemberFromProjectCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public RemoveMemberFormProjectCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        public async Task<bool> Handle(RemoveMemberFromProjectCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này");
            var project = await _context.Projects
                .Include(p => p.Workspace)
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.Role)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
                ?? throw new NotFoundException("Project không tồn tại");
            var isWorkspaceOwner = project.Workspace.OwnerId == currentUserId;
            var isProjectLeader = project.ProjectMembers.Any(pm => pm.UserId == currentUserId && pm.Role.Name == "ProjectLeader");
            var isSelfRemove = request.MemberId == currentUserId;

            // Người thực hiện phải là WorkspaceOwner, ProjectLeader hoặc tự rời dự án
            if (!isWorkspaceOwner && !isProjectLeader && !isSelfRemove)
            {
                throw new ForbiddenException("Bạn không có quyền xóa thành viên này khỏi dự án.");
            }
            var targetMember = project.ProjectMembers.FirstOrDefault(u => u.UserId == request.MemberId)
                ?? throw new NotFoundException("Thành viên không tồn tại trong dự án này");
            if (targetMember.Role.Name == "ProjectLeader" && isSelfRemove && !isWorkspaceOwner)
            {
                throw new BadRequestException("Trưởng dự án không thể tự rời dự án. Vui lòng chuyển giao vai trò Trưởng dự án cho thành viên khác trước");
            }
            _context.ProjectMembers.Remove(targetMember);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
