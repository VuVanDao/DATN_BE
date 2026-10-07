using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Modules.Workspaces.Application.DTOs;
using DATN_BE.Modules.Workspaces.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Workspaces.Application.Commands.AddWorkspaceMember
{
    public class AddMemberToWorkspaceCommandHandler : IRequestHandler<AddMemberToWorkspaceCommand, AddWorkspaceMembersResultDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public AddMemberToWorkspaceCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        public async Task<AddWorkspaceMembersResultDto> Handle(AddMemberToWorkspaceCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này");

            // 1. check workspace exist
            var workspace = await _context.Workspaces.Include(w => w.Members).Include(w => w.Owner)
                .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && !w.IsDeleted, cancellationToken) ?? throw new NotFoundException("Workspace này không tồn tại");

            // 2. check role user who act add must be WorkspaceOwner
            var isOwner = workspace.Owner.Id == currentUserId;
            if (!isOwner)
            {
                throw new ForbiddenException("Chỉ Người sở hữu mới có quyền thêm thành viên vào Workspace này.");
            }
            // 3. lay danh sach role thuoc scope workspace
            var workspaceRoles = await _context.Roles
                .Where(r => r.Scope == RoleScope.Workspace && r.Name != "WorkspaceOwner")
                .ToListAsync(cancellationToken);
            // lay role mac dinh la WorkspaceMember
            var defaultRole = workspaceRoles
                .FirstOrDefault(r => r.Name == "WorkspaceMember") 
                    ?? throw new NotFoundException("Không tìm thấy vai trò WorkspaceMember trong hệ thống.");

            // 4. lay danh sach cac member hien tai check trung lap
            var existingUserIds = workspace.Members
                .Select(m => m.UserId)
                .ToHashSet();
            // 5. xu ly tung thanh vien
            var results = new List<MemberAddResult>();

            foreach (var entry in request.Members)
            {
                var memberEmail = entry.Email.Trim();
                var result = new MemberAddResult { Email = memberEmail };

                try
                {
                    // tim user theo email
                    var targetUser = await _context.Users.
                        FirstOrDefaultAsync(u => u.Email.ToLower() == memberEmail.ToLower() && u.IsActive == true, cancellationToken);

                    if(targetUser == null)
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
                        results.Add(result );
                        continue;
                    }
                    // xac dinh role
                    var role = defaultRole;
                    if (entry.RoleId.HasValue)
                    {
                        var targetRole = workspaceRoles.FirstOrDefault(r=>r.Id == entry.RoleId);
                        if(targetRole == null)
                        {
                            result.Success = false;
                            result.Message = "Vai trò không hợp lệ hoặc không thuộc phạm vi Workspace";
                            results.Add(result);
                            continue;
                        }
                        role = targetRole;
                    }
                    // tao workspaceMember, chap nhan thanh vien vao role
                    var workspaceMember = new WorkspaceMember
                    {
                        WorkspaceId = workspace.Id,
                        UserId = targetUser.Id,
                        RoleId = role.Id
                    };
                    _context.WorkspaceMembers.Add(workspaceMember);
                    existingUserIds.Add(targetUser.Id); // Đưa vào set để tránh trùng nếu trong cùng batch có 2 dòng cùng email

                    result.Success = true;
                    result.Message = "Thêm thành viên thành công";
                    result.Member = new WorkspaceMemberDto
                    {
                        UserId = targetUser.Id,
                        Username = targetUser.Username,
                        Email = targetUser.Email,
                        AvatarUrl = targetUser.AvatarUrl,
                        RoleId = role.Id,
                        RoleName = role.Name,
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
