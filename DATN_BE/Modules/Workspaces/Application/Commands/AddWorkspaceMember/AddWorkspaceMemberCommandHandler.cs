using DATN_BE.Modules.Workspaces.Application.DTOs;
using DATN_BE.Modules.Workspaces.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Workspaces.Application.Commands.AddWorkspaceMember
{
    public class AddWorkspaceMemberCommandHandler : IRequestHandler<AddWorkspaceMemberCommand, WorkspaceMemberDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public AddWorkspaceMemberCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        public async Task<WorkspaceMemberDto> Handle(AddWorkspaceMemberCommand request, CancellationToken cancellationToken)
        {
            Console.WriteLine("check vao ham");

            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này.");
            // 1. check workspace exist
            var workspace = await _context.Workspaces.Include(w => w.Members).Include(w => w.Owner)
                .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && !w.IsDeleted, cancellationToken) ?? throw new NotFoundException("Workspace này không tồn tại");
            // 2. check role user who act add must be WorkspaceOwner
            var isOwner = workspace.Owner.Id == currentUserId;
            if (!isOwner)
            {
                throw new ForbiddenException("Chỉ Người sở hữu mới có quyền thêm thành viên vào Workspace này.");
            }
            // 3.find user who need to be add by email
            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && !u.IsDeleted && u.IsActive == true, cancellationToken) 
                    ?? throw new NotFoundException($"Không tìm thấy người dùng có email {request.Email}");
            // 4. Kiểm tra xem người này đã là thành viên chưa
            var checkUserIncluded = await _context.WorkspaceMembers.AnyAsync(wm => wm.WorkspaceId == request.WorkspaceId && wm.UserId == targetUser.Id, cancellationToken);
            if (checkUserIncluded)
            {
                throw new BadRequestException("Người dùng này đã là thành viên trong Workspace rồi.");
            }

            // 5. Xác định Role (nếu không truyền RoleId thì mặc định lấy Role Developer)
            Guid roleId;
            string roleName = "Developer";
            if (request.RoleId.HasValue)
            {
                var targetRole = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken) ?? throw new NotFoundException($"Không tìm thấy vai trò trong hệ thống");
                roleId = targetRole.Id;
                roleName = targetRole.Name;
            }
            else
            {
                var defaultRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == "Developer", cancellationToken) ?? throw new NotFoundException($"Không tìm thấy vai trò Developer trong hệ thống");
                roleId = defaultRole.Id;
                roleName = defaultRole.Name;
            }
            // 6. Thêm vào bảng WorkspaceMembers
            var workspaceMember = new WorkspaceMember
            {
                WorkspaceId = workspace.Id,
                UserId = targetUser.Id,
                RoleId = roleId
            };
            _context.WorkspaceMembers.Add(workspaceMember);
            await _context.SaveChangesAsync(cancellationToken);

            return new WorkspaceMemberDto
            {
                UserId = targetUser.Id,
                Username = targetUser.Username,
                Email = targetUser.Email,
                AvatarUrl = targetUser.AvatarUrl,
                RoleId = roleId,
                RoleName = roleName,
                JoinAt = workspaceMember.JoinAt
            };
        }
    }
}
