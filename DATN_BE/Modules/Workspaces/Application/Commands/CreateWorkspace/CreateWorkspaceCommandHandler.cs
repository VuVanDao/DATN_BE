using DATN_BE.Modules.Workspaces.Application.DTOs;
using DATN_BE.Modules.Workspaces.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Utils;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Workspaces.Application.Commands.CreateWorkspace
{
    public class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, WorkspaceDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public CreateWorkspaceCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        public async Task<WorkspaceDto> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId ?? throw new ForbiddenException("Bạn cần đăng nhập để tạo Workspace.");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId && !u.IsDeleted, cancellationToken) ?? throw new NotFoundException("Người dùng không tồn tại");
            // 1. Tự động sinh Slug và xử lý chống trùng Slug
            var baseSlug = SlugHelper.GenerateSlug(request.Name);
            var existingSlugs = await _context.Workspaces
                .Where(w => w.Slug.StartsWith(baseSlug))
                .Select(w => w.Slug)
                .ToListAsync(cancellationToken);
            var slugSet = new HashSet<string>(existingSlugs);
            var slug = baseSlug;
            var counter = 1;
            while (slugSet.Contains(slug))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }
            // 2. Tạo Workspace
            var workspace = new Workspace
            {
                Name = request.Name.Trim(),
                Slug = slug,
                Description = request.Description,
                OwnerId = currentUserId
            };
            _context.Workspaces.Add(workspace);
            // 3. Tìm Role "WorkspaceOwner" để gán cho Người tạo
            var OwnerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "WorkspaceOwner", cancellationToken) 
                ?? throw new NotFoundException("Không tìm thấy vai trò WorkspaceOwner");
            // 4. Thêm người tạo vào danh sách WorkspaceMembers
            var member = new WorkspaceMember
            {
                WorkspaceId = workspace.Id,
                UserId = currentUserId,
                RoleId = OwnerRole.Id
            };
            _context.WorkspaceMembers.Add(member);
            await _context.SaveChangesAsync(cancellationToken);
            return new WorkspaceDto
            {
                Id = workspace.Id,
                Name = workspace.Name,
                Slug = workspace.Slug,
                Description = workspace.Description,
                LogoUrl = workspace.LogoUrl,
                CoverImgUrl = workspace.CoverImgUrl,
                OwnerId = workspace.OwnerId,
                OwnerName = user.Username,
                MembersCount = 1,
                ProjectsCount = 0,
                CurrentUserRole = OwnerRole != null? OwnerRole.Name : "WorkspaceOwner",
                CreatedAt = workspace.CreatedAt
            };
        }
    }
}
