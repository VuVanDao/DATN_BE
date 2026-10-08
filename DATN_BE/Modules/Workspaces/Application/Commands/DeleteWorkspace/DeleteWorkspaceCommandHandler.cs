using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Workspaces.Application.Commands.DeleteWorkspace
{
    public class DeleteWorkspaceCommandHandler : IRequestHandler<DeleteWorkspaceCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeleteWorkspaceCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra xác thực người dùng hiện tại
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này.");

            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == currentUserId && u.IsActive, cancellationToken)
                ?? throw new NotFoundException("Người dùng không tồn tại hoặc đã bị vô hiệu hóa.");

            // 2. Tìm Workspace
            var currentWorkspace = await _context.Workspaces
                .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy Workspace hoặc Workspace đã bị xoá.");

            // 3. Kiểm tra phân quyền: Chỉ SystemAdmin hoặc WorkspaceOwner mới được xóa
            var isOwner = currentWorkspace.OwnerId == currentUserId;
            if (!isOwner)
            {
                throw new ForbiddenException("Chỉ chủ sở hữu Workspace mới có quyền xóa Workspace này.");
            }

            // 4. Batch Update trực tiếp tại CSDL qua Navigation Property (không cần query trung gian lấy ID)
            // Lưu ý: Cần cập nhật từ dưới lên (Bottom-Up: Cards -> Columns -> Boards -> Projects -> Workspace)
            // hoặc dùng IgnoreQueryFilters() để tránh Global Query Filter (IsDeleted = false) chặn các bảng cha
            var now = DateTimeOffset.UtcNow;

            // Xóa Cards thuộc Workspace này
            await _context.Cards
                .IgnoreQueryFilters()
                .Where(c => c.Column.Board.Project.WorkspaceId == currentWorkspace.Id && !c.IsDeleted)
                .ExecuteUpdateAsync(card => card
                    .SetProperty(c => c.IsDeleted, true)
                    .SetProperty(c => c.DeletedAt, now), cancellationToken);

            // Xóa Columns thuộc Workspace này
            await _context.Columns
                .IgnoreQueryFilters()
                .Where(col => col.Board.Project.WorkspaceId == currentWorkspace.Id && !col.IsDeleted)
                .ExecuteUpdateAsync(column => column
                    .SetProperty(col => col.IsDeleted, true)
                    .SetProperty(col => col.DeletedAt, now), cancellationToken);

            // Xóa Boards thuộc Workspace này
            await _context.Boards
                .IgnoreQueryFilters()
                .Where(b => b.Project.WorkspaceId == currentWorkspace.Id && !b.IsDeleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.IsDeleted, true)
                    .SetProperty(b => b.DeletedAt, now), cancellationToken);

            // Xóa Sprints thuộc Workspace này
            await _context.Sprints
                .IgnoreQueryFilters()
                .Where(sp => sp.Project.WorkspaceId == currentWorkspace.Id && !sp.IsDeleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(sp => sp.IsDeleted, true)
                    .SetProperty(sp => sp.DeletedAt, now), cancellationToken);

            // Xóa Projects thuộc Workspace này
            await _context.Projects
                .IgnoreQueryFilters()
                .Where(p => p.WorkspaceId == currentWorkspace.Id && !p.IsDeleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.IsDeleted, true)
                    .SetProperty(p => p.DeletedAt, now), cancellationToken);

            // Xóa Workspace
            currentWorkspace.IsDeleted = true;
            currentWorkspace.DeletedAt = now;

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
