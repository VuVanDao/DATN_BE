using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.AddMemberToProejct
{
    public class AddMemberToProejctCommandHandler : IRequestHandler<AddMemberToProejctCommand, List<ProjectMemberDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public AddMemberToProejctCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        public async Task<List<ProjectMemberDto>> Handle(AddMemberToProejctCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này");
            var project = await _context.Projects
                .Include(p => p.Workspace)
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.Role)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
                ?? throw new NotFoundException("Project không tồn tại");
            // check permission
            var isWorkspaceOwner = project.Workspace.OwnerId == currentUserId;
            var isProjectLeader = project.ProjectMembers.Any(pm => pm.UserId == currentUserId && pm.Role.Name == "ProjectLeader");
            if (!isWorkspaceOwner && !isProjectLeader)
            {
                throw new ForbiddenException("Chỉ Chủ sở hữu Workspace hoặc Trưởng dự án mới có quyền thêm thành viên");
            }
            // get valid role
            var projectRoles = await _context.Roles
                .Where(r => r.Scope == RoleScope.Project)
                .ToListAsync(cancellationToken);
            // role mac dinh neu FE khong gui role cho member nay len
            var defaultMemberRole = projectRoles.FirstOrDefault(r => r.Name == "ProjectMember")
                ?? throw new NotFoundException("Không tìm thấy vai trò 'ProjectMember' trong hệ thống");
            // get leader role    
            var leaderRole = projectRoles.FirstOrDefault(r => r.Name == "ProjectLeader")
                ?? throw new NotFoundException("Không tìm thấy vai trò 'ProjectLeader' trong hệ thống");
            if (request.Members.Count(m => m.RoleId == leaderRole.Id) > 1)
            {
                throw new BadRequestException("Mỗi lần thêm thành viên chỉ được chỉ định một Trưởng dự án");
            }
            // list valid userId in workspace
            var validWorkspaceUserIds = await _context.WorkspaceMembers
                                                .Where(wm => wm.WorkspaceId == project.WorkspaceId)
                                                .Select(wm => wm.UserId)
                                                .ToHashSetAsync(cancellationToken);
            // list member in project
            var existingProjectUserIds = project.ProjectMembers.Select(pm => pm.UserId).ToHashSet();

            // list member added successful
            var addedMembers = new List<ProjectMember>();

            foreach (var entry in request.Members)
            {
                // check user belong to workspace
                var isUserBelongToWorkspace = validWorkspaceUserIds.Any(wsui => wsui == entry.UserId);
                if (!isUserBelongToWorkspace)
                {
                    throw new BadRequestException($"Người dùng với Id '{entry.UserId}' không thuộc Không gian làm việc này");
                }
                // check if user already in project
                var userAlreadyInProject = existingProjectUserIds.Contains(entry.UserId);
                if (userAlreadyInProject)
                {
                    continue;
                }
                // define role
                var targetRoleId = defaultMemberRole.Id;
                if (entry.RoleId.HasValue)
                {
                    var selectedRole = projectRoles.FirstOrDefault(r => r.Id == entry.RoleId.Value)
                        ?? throw new BadRequestException("Vai trò được chọn không hợp lệ cho phạm vi Dự án");
                    targetRoleId = selectedRole.Id;
                }
                // if role is leader
                // make oleProjectLeader down to member
                if (targetRoleId == leaderRole.Id)
                {
                    foreach (var currentLeader in project.ProjectMembers.Where(pm => pm.RoleId == leaderRole.Id))
                    {
                        currentLeader.RoleId = defaultMemberRole.Id;
                    }
                }
                var newMember = new ProjectMember
                {
                    ProjectId = project.Id,
                    UserId = entry.UserId,
                    RoleId = targetRoleId,
                    JoinedAt = DateTimeOffset.UtcNow
                };
                _context.ProjectMembers.Add(newMember);
                addedMembers.Add(newMember);
                existingProjectUserIds.Add(entry.UserId);
            }
            await _context.SaveChangesAsync(cancellationToken);

            // return list new member added
            var roleNameById = projectRoles.ToDictionary(r => r.Id, r => r.Name);
            
            // ids list member requested to add
            var requestedUserIds = request.Members.Select(m => m.UserId).ToHashSet();
            var usersById = await _context.Users
                .Where(u => requestedUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, cancellationToken);
             return addedMembers.Select(m => new ProjectMemberDto
                {
                    UserId = m.UserId,
                    Username = usersById[m.UserId].Username,
                    Email = usersById[m.UserId].Email,
                    AvatarUrl = usersById[m.UserId].AvatarUrl,
                    RoleId = m.RoleId,
                    RoleName = roleNameById[m.RoleId],
                    JoinedAt = m.JoinedAt
                }).ToList();
        }
    }
}
