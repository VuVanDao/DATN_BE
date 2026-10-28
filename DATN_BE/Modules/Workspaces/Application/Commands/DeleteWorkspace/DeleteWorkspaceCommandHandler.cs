using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain;
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

            // Track the cascade so project deletions and audit rows share the same SaveChanges transaction.
            // Ignore filters to include active descendants beneath previously deleted parents.
            var now = DateTimeOffset.UtcNow;

            var cards = await _context.Cards
                .IgnoreQueryFilters()
                .Where(c => c.Column.Board.Project.WorkspaceId == currentWorkspace.Id && !c.IsDeleted)
                .ToListAsync(cancellationToken);

            var columns = await _context.Columns
                .IgnoreQueryFilters()
                .Where(col => col.Board.Project.WorkspaceId == currentWorkspace.Id && !col.IsDeleted)
                .ToListAsync(cancellationToken);

            var boards = await _context.Boards
                .IgnoreQueryFilters()
                .Where(b => b.Project.WorkspaceId == currentWorkspace.Id && !b.IsDeleted)
                .ToListAsync(cancellationToken);

            var sprints = await _context.Sprints
                .IgnoreQueryFilters()
                .Where(sp => sp.Project.WorkspaceId == currentWorkspace.Id && !sp.IsDeleted)
                .ToListAsync(cancellationToken);

            var projects = await _context.Projects
                .IgnoreQueryFilters()
                .Where(p => p.WorkspaceId == currentWorkspace.Id && !p.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var entity in cards.Cast<ISoftDelete>().Concat(columns).Concat(boards).Concat(sprints).Concat(projects))
            {
                entity.IsDeleted = true;
                entity.DeletedAt = now;
            }

            // Xóa Workspace
            currentWorkspace.IsDeleted = true;
            currentWorkspace.DeletedAt = now;

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
