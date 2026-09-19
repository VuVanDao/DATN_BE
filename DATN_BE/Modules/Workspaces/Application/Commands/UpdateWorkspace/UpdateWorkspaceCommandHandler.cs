using DATN_BE.Modules.Workspaces.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Utils;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Workspaces.Application.Commands.UpdateWorkspace
{
    public class UpdateWorkspaceCommandHandler : IRequestHandler<UpdateWorkspaceCommand, WorkspaceDetailDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICloudinaryService _cloudinaryService;
        public UpdateWorkspaceCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _cloudinaryService = cloudinaryService;
        }
        public async Task<WorkspaceDetailDto> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
        {
            var currUserId = _currentUserService.UserId ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này");

            var workspace = await _context.Workspaces
                .Include(w => w.Owner)
                .Include(w => w.Members).ThenInclude(mem => mem.User)
                .Include(w => w.Members).ThenInclude(mem => mem.Role)
                .Include(w => w.Projects).ThenInclude(proj => proj.ProjectMembers).ThenInclude(pm => pm.User)
                .Include(w => w.Projects).ThenInclude(proj => proj.Boards).ThenInclude(b => b.Columns).ThenInclude(col => col.Cards)
                .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, cancellationToken) ?? throw new NotFoundException("Workspace không tồn tại");
            // check user co phai workspaceOwner ko
            var isWorkspaceOwner = workspace.OwnerId == currUserId;
            if(isWorkspaceOwner == false)
            {
                throw new ForbiddenException("Chỉ người sở hữu mới có quyền thay đổi thông tin Workspace");
            }
            // doi ten workspace = generate lai slug va choon trung slug
            if(!string.IsNullOrWhiteSpace(request.Name) && request.Name.Trim() != workspace.Name)
            {
                workspace.Name = request.Name.Trim();
                var baseSlug = SlugHelper.GenerateSlug(workspace.Name);
                var existSlug = await _context.Workspaces
                    .Where(w => w.Id != workspace.Id && w.Slug.StartsWith(baseSlug))
                    .Select(w => w.Slug)
                    .ToListAsync(cancellationToken);
                var slugSet = new HashSet<string>(existSlug);
                var slug = baseSlug;
                int counter = 1;
                while (slugSet.Contains(slug))
                {
                    slug = $"{baseSlug}-{counter}";
                    counter++;
                }
                workspace.Slug = slug;
            }
            // update description
            if (request.Description != null)
            {
                workspace.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            }
            // upload logo
            if(request.Logo != null && request.Logo.Length > 0)
            {
                var logoUrl = await _cloudinaryService.UploadWorkspaceImageAsync(request.Logo, workspace.Id.ToString(), "logo", cancellationToken);
                workspace.LogoUrl = logoUrl;
            }
            //upload cover img
            if (request.CoverImage != null && request.CoverImage.Length > 0)
            {
                var coverUrl = await _cloudinaryService.UploadWorkspaceImageAsync(
                    request.CoverImage,
                    workspace.Id.ToString(),
                    "cover",
                    cancellationToken);
                workspace.CoverImgUrl = coverUrl;
            }
            await _context.SaveChangesAsync(cancellationToken);

            var myMembership = workspace.Members.FirstOrDefault(m => m.UserId == currUserId);
            var myRole = workspace.OwnerId == currUserId ? "WorkspaceOwner" : myMembership?.Role?.Name ?? "Member";

            return new WorkspaceDetailDto
            {
                Id = workspace.Id,
                Name = workspace.Name,
                Slug = workspace.Slug,
                Description = workspace.Description,
                LogoUrl = workspace.LogoUrl,
                CoverImageUrl = workspace.CoverImgUrl,
                OwnerId = workspace.OwnerId,
                OwnerName = workspace.Owner?.Username ?? string.Empty,
                CurrentUserRole = myRole,
                CreatedAt = workspace.CreatedAt,
                Members = workspace.Members.Select(m => new WorkspaceMemberDto
                {
                    UserId = m.UserId,
                    Username = m.User?.Username ?? string.Empty,
                    Email = m.User?.Email ?? string.Empty,
                    AvatarUrl = m.User?.AvatarUrl,
                    RoleId = m.RoleId,
                    RoleName = m.Role?.Name ?? string.Empty,
                    JoinAt = m.JoinAt
                }).ToList(),
                Projects = workspace.Projects
                  .Where(p => !p.IsDeleted)
                  .OrderByDescending(p => p.CreatedAt)
                  .Select(p => new WorkspaceProjectDto
                  {
                      Id = p.Id,
                      Name = p.Name,
                      ProjectKey = p.ProjectKey,
                      Description = p.Description,
                      MembersCount = p.ProjectMembers.Count,
                      TasksCount = p.Boards.SelectMany(b => b.Columns).SelectMany(c => c.Cards).Count(c => !c.IsDeleted),
                      Members = p.ProjectMembers.Select(pm => new ProjectMemberAvatarDto
                      {
                          UserId = pm.UserId,
                          Username = pm.User?.Username ?? string.Empty,
                          AvatarUrl = pm.User?.AvatarUrl
                      }).ToList(),
                      CreatedAt = p.CreatedAt
                  }).ToList()
            };
        }
    }
}
