using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
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
            var isProjectLeader = project.ProjectMembers.Any(pm => pm.UserId == currentUserId && pm.Role.Name == RoleConstants.ProjectLeader);
            if (!isWorkspaceOwner && !isProjectLeader)
            {
                throw new ForbiddenException("Chỉ Chủ sở hữu Workspace hoặc trưởng dự án mới có quyền thêm thành viên");
            }

            // get valid role
            var projectRolesAvailable = await _context.Roles
                .Where(r => r.Scope == RoleScope.Project)
                .ToListAsync(cancellationToken);

            // role mac dinh neu FE khong gui role cho member nay len
            var defaultMemberRole = projectRolesAvailable.FirstOrDefault(r => r.Name == RoleConstants.ProjectMember)
                ?? throw new NotFoundException($"Không tìm thấy vai trò {RoleConstants.ProjectMember} trong hệ thống");

            // get leader role    
            var leaderRole = projectRolesAvailable.FirstOrDefault(r => r.Name == RoleConstants.ProjectLeader)
                ?? throw new NotFoundException($"Không tìm thấy vai trò {RoleConstants.ProjectLeader} trong hệ thống");

            if (request.Members.Count(m => m.RoleId == leaderRole.Id) > 1)
            {
                throw new BadRequestException("Mỗi lần thêm thành viên chỉ được chỉ định một Trưởng dự án");
            }

            // list valid userId in workspace
            var validWorkspaceUserIds = await _context.WorkspaceMembers
                                                .Where(wm => wm.WorkspaceId == project.WorkspaceId)
                                                .Select(wm => wm.UserId)
                                                .ToHashSetAsync(cancellationToken);
            // list member in current project
            var existingProjectUserIds = project.ProjectMembers.Select(pm => pm.UserId).ToHashSet();

            // list member added successful
            var addedMembersResult = new List<ProjectMember>();

            foreach (var member in request.Members)
            {
                // check user belong to workspace
                var isUserBelongToWorkspace = validWorkspaceUserIds.Any(wsui => wsui == member.UserId);
                if (!isUserBelongToWorkspace)
                {
                    throw new BadRequestException($"Người dùng với Id '{member.UserId}' không thuộc Không gian làm việc này");
                }
                // check if user already in project
                var userAlreadyInProject = existingProjectUserIds.Contains(member.UserId);
                if (userAlreadyInProject)
                {
                    continue;
                }
                // define role
                var targetRoleId = defaultMemberRole.Id;
                if (member.RoleId.HasValue)
                {
                    var selectedRole = projectRolesAvailable.FirstOrDefault(r => r.Id == member.RoleId.Value)
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
                    UserId = member.UserId,
                    RoleId = targetRoleId,
                    JoinedAt = DateTimeOffset.UtcNow
                };
                _context.ProjectMembers.Add(newMember);
                addedMembersResult.Add(newMember);
                existingProjectUserIds.Add(member.UserId);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // return list new member added
            var roleNameById = projectRolesAvailable.ToDictionary(r => r.Id, r => r.Name);
            
            // ids list member requested to add
            var requestedUserIds = request.Members.Select(m => m.UserId).ToHashSet();
            var usersById = await _context.Users
                .Where(u => requestedUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, cancellationToken);
             return addedMembersResult.Select(m => new ProjectMemberDto
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
