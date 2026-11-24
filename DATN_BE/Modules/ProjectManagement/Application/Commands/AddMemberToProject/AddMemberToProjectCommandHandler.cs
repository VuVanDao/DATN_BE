using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Modules.Notifications.Domain.Enum;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using DATN_BE.Shared.Enum;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.AddMemberToProejct
{
    public class AddMemberToProejctCommandHandler : IRequestHandler<AddMemberToProejctCommand, List<ProjectMemberDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService? _notificationService;

        public AddMemberToProejctCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            INotificationService? notificationService = null)
        {
            _context = context;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
        }

        public async Task<List<ProjectMemberDto>> Handle(AddMemberToProejctCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new UnauthorizedException("Bạn cần đăng nhập để thực hiện hành động này");

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

            // Role mặc định cho thành viên mới khi thêm vào dự án
            var projectMemberRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == RoleConstants.ProjectMember && r.Scope == RoleScope.Project, cancellationToken)
                ?? throw new NotFoundException($"Không tìm thấy vai trò {RoleConstants.ProjectMember} trong hệ thống");

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
                var isUserBelongToWorkspace = validWorkspaceUserIds.Contains(member.UserId);
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

                var newMember = new ProjectMember
                {
                    ProjectId = project.Id,
                    UserId = member.UserId,
                    RoleId = projectMemberRole.Id,
                    JobTitle = string.IsNullOrWhiteSpace(member.JobTitle) ? null : member.JobTitle.Trim(),
                    JoinedAt = DateTimeOffset.UtcNow
                };

                _context.ProjectMembers.Add(newMember);
                addedMembersResult.Add(newMember);
                existingProjectUserIds.Add(member.UserId);
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (_notificationService != null)
            {
                var recipientMembers = addedMembersResult
                    .Where(m => m.UserId != currentUserId)
                    .ToList();

                if (recipientMembers.Count > 0)
                {
                    var actor = await _context.Users
                        .AsNoTracking()
                        .Where(u => u.Id == currentUserId)
                        .Select(u => new { u.Username, u.AvatarUrl })
                        .FirstOrDefaultAsync(cancellationToken);

                    var actorName = !string.IsNullOrWhiteSpace(actor?.Username)
                        ? actor.Username
                        : _currentUserService.Username ?? "Quản lý dự án";

                    var targetUrl = !string.IsNullOrWhiteSpace(project.ProjectKey)
                        ? $"/workspaces/{project.WorkspaceId}/projects/{project.ProjectKey}"
                        : null;

                    var notificationItems = recipientMembers.Select(m => new NotificationDispatchItem(
                        ReceiverId: m.UserId,
                        SenderId: currentUserId,
                        SenderName: actorName,
                        SenderAvatarUrl: actor?.AvatarUrl,
                        Type: NotificationType.ProjectInvite,
                        Title: "Được thêm vào dự án",
                        Content: $"{actorName} đã thêm bạn vào dự án \"{project.Name}\" ({project.ProjectKey})",
                        EntityType: EntityType.Project,
                        EntityId: project.Id,
                        TargetUrl: targetUrl));

                    await _notificationService.CreateAndSendManyAsync(notificationItems, cancellationToken);
                }
            }

            // return list new member added
            var addedUserIds = addedMembersResult.Select(m => m.UserId).ToHashSet();
            var usersById = await _context.Users
                .Where(u => addedUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, cancellationToken);

            return addedMembersResult.Select(m => new ProjectMemberDto
            {
                UserId = m.UserId,
                Username = usersById.TryGetValue(m.UserId, out var u) ? u.Username : string.Empty,
                Email = usersById.TryGetValue(m.UserId, out var u2) ? u2.Email : string.Empty,
                AvatarUrl = usersById.TryGetValue(m.UserId, out var u3) ? u3.AvatarUrl : null,
                RoleId = projectMemberRole.Id,
                RoleName = projectMemberRole.Name,
                JobTitle = m.JobTitle,
                JoinedAt = m.JoinedAt
            }).ToList();
        }
    }
}
