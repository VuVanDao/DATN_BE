using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
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
                ?? throw new UnauthorizedException("Bạn cần đăng nhập để thực hiện hành động này");

            var project = await _context.Projects
                .Include(p => p.Workspace)
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.Role)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
                ?? throw new NotFoundException("Project không tồn tại");

            var isWorkspaceOwner = project.Workspace.OwnerId == currentUserId;

            var isMemberInProject = project.ProjectMembers.Any(pm => pm.UserId == currentUserId);
            if (!isMemberInProject && !isWorkspaceOwner)
            {
                throw new ForbiddenException("Bạn không phải thành viên của dự án này");
            }

            var isProjectLeader = project.ProjectMembers.Any(pm => pm.UserId == currentUserId && pm.Role.Name == RoleConstants.ProjectLeader);
            var isSelfRemove = request.MemberId == currentUserId;

            // Người thực hiện phải là WorkspaceOwner, ProjectLeader hoặc tự rời dự án
            if (!isWorkspaceOwner && !isProjectLeader && !isSelfRemove)
            {
                throw new ForbiddenException("Bạn không có quyền thực hiện hành vi này ");
            }

            var targetMember = project.ProjectMembers.FirstOrDefault(u => u.UserId == request.MemberId)
                ?? throw new NotFoundException("Thành viên không tham gia dự án này");

            if (targetMember.Role.Name == RoleConstants.ProjectLeader )
            {
                throw new BadRequestException("Mỗi dự án cần ít nhất 1 Trưởng dự án. Vui lòng chuyển giao vai trò Trưởng dự án cho thành viên khác trước");
            }

            var cardsAssignedToMember = await _context.Cards
                 .Where(c => c.Column.Board.ProjectId == project.Id && c.AssigneeId == request.MemberId)
                 .ToListAsync(cancellationToken);

            foreach (var card in cardsAssignedToMember)
            {
                card.AssigneeId = null;
            }

            _context.ProjectMembers.Remove(targetMember);

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
