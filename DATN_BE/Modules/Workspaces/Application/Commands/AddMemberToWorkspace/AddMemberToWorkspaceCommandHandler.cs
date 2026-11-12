using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Modules.Notifications.Domain.Enum;
using DATN_BE.Modules.Workspaces.Application.DTOs;
using DATN_BE.Modules.Workspaces.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using DATN_BE.Shared.Enum;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Workspaces.Application.Commands.AddWorkspaceMember
{
    public class AddMemberToWorkspaceCommandHandler : IRequestHandler<AddMemberToWorkspaceCommand, AddWorkspaceMembersResultDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService? _notificationService;

        public AddMemberToWorkspaceCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            INotificationService? notificationService = null)
        {
            _context = context;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
        }

        public async Task<AddWorkspaceMembersResultDto> Handle(AddMemberToWorkspaceCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này");

            // 1. check workspace exist
            var workspace = await _context.Workspaces.Include(w => w.Members).Include(w => w.Owner)
                .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && !w.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Workspace này không tồn tại");

            // 2. check role user who act add must be WorkspaceOwner
            var isOwner = workspace.Owner.Id == currentUserId;
            if (!isOwner)
            {
                throw new ForbiddenException("Chỉ Người sở hữu mới có quyền thêm thành viên vào Workspace này.");
            }

            // 3. Lấy vai trò WorkspaceMember (tất cả thành viên mới vào workspace đều có vai trò này)
            var workspaceMemberRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Scope == RoleScope.Workspace && r.Name == RoleConstants.WorkspaceMember, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy vai trò WorkspaceMember trong hệ thống.");

            // 4. lay danh sach cac member hien tai check trung lap
            var existingUserIds = workspace.Members
                .Select(m => m.UserId)
                .ToHashSet();

            // 5. xu ly tung thanh vien
            var results = new List<MemberAddResult>();
            var addedUsers = new List<User>();

            foreach (var entry in request.Members)
            {
                var memberEmail = entry.Email.Trim();
                var result = new MemberAddResult { Email = memberEmail };

                try
                {
                    // tim user theo email
                    var targetUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email.ToLower() == memberEmail.ToLower() && u.IsActive == true, cancellationToken);

                    if (targetUser == null)
                    {
                        result.Success = false;
                        result.Message = $"Không tìm thấy người dùng với email '{memberEmail}'";
                        results.Add(result);
                        continue;
                    }

                    // kiem tra user da la thanh vien cua workspace chua
                    if (existingUserIds.Contains(targetUser.Id))
                    {
                        result.Success = false;
                        result.Message = "Người dùng này đã là thành viên trong Workspace";
                        results.Add(result);
                        continue;
                    }

                    // tao workspaceMember
                    var workspaceMember = new WorkspaceMember
                    {
                        WorkspaceId = workspace.Id,
                        UserId = targetUser.Id,
                        RoleId = workspaceMemberRole.Id
                    };
                    _context.WorkspaceMembers.Add(workspaceMember);
                    existingUserIds.Add(targetUser.Id); // Đưa vào set để tránh trùng nếu trong cùng batch có 2 dòng cùng email
                    addedUsers.Add(targetUser);

                    result.Success = true;
                    result.Message = "Thêm thành viên thành công";
                    result.Member = new WorkspaceMemberDto
                    {
                        UserId = targetUser.Id,
                        Username = targetUser.Username,
                        Email = targetUser.Email,
                        AvatarUrl = targetUser.AvatarUrl,
                        RoleId = workspaceMemberRole.Id,
                        RoleName = workspaceMemberRole.Name,
                        JoinAt = workspaceMember.JoinAt
                    };
                }
                catch (Exception)
                {
                    result.Success = false;
                    result.Message = "Đã xảy ra lỗi khi xử lý thành viên này.";
                }
                results.Add(result);
            }

            // 6. Lưu toàn bộ các member hợp lệ vào Database
            await _context.SaveChangesAsync(cancellationToken);

            // 7. Gửi thông báo cho các thành viên mới được thêm vào Workspace
            if (_notificationService != null && addedUsers.Count > 0)
            {
                var actorName = string.IsNullOrWhiteSpace(workspace.Owner.Username)
                    ? "Chủ sở hữu"
                    : workspace.Owner.Username;

                var targetUrl = string.IsNullOrWhiteSpace(workspace.Slug)
                    ? null
                    : $"/workspaces/{workspace.Slug}";

                var notificationItems = addedUsers
                    .Where(user => user.Id != currentUserId)
                    .Select(user => new NotificationDispatchItem(
                        ReceiverId: user.Id,
                        SenderId: currentUserId,
                        SenderName: actorName,
                        SenderAvatarUrl: workspace.Owner.AvatarUrl,
                        Type: NotificationType.WorkspaceInvite,
                        Title: "Được thêm vào không gian làm việc",
                        Content: $"{actorName} đã thêm bạn vào không gian làm việc \"{workspace.Name}\"",
                        EntityType: EntityType.Workspace,
                        EntityId: workspace.Id,
                        TargetUrl: targetUrl));

                await _notificationService.CreateAndSendManyAsync(notificationItems, cancellationToken);
            }

            return new AddWorkspaceMembersResultDto
            {
                TotalRequested = request.Members.Count,
                TotalSuccess = results.Count(r => r.Success),
                TotalFailed = results.Count(r => !r.Success),
                Results = results
            };
        }
    }
}
