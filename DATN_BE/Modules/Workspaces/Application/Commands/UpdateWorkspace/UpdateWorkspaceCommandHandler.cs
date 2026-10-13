using DATN_BE.Modules.Workspaces.Application.DTOs;
using DATN_BE.Modules.Workspaces.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
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
            var workspaceToUpdate = await _context.Workspaces.FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, cancellationToken) ?? throw new NotFoundException("Không tìm thấy workspace cần cập nhật");
            if (currUserId != workspaceToUpdate.OwnerId)
            {
                throw new ForbiddenException("Người dùng không có quyền thực hiện hành động này");
            }
            workspaceToUpdate.Description = request.Description?.Trim();
            await GenerateNewUniqueSlugtoUpdate(workspaceToUpdate, request.Name, cancellationToken);
            await UploadImagesAsync(workspaceToUpdate, request, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            var res = await _context.Workspaces.AsNoTracking().Select(w => new WorkspaceDetailDto
            {
                Name = w.Name,
                Id = w.Id,
                Slug = w.Slug,
                OwnerId = w.OwnerId,
                OwnerName = w.Owner.Username,
                LogoUrl = w.LogoUrl,
                CoverImageUrl = w.CoverImgUrl,
                Description = w.Description,
                CreatedAt = w.CreatedAt,
                CurrentUserRole = w.OwnerId == currUserId ? RoleConstants.WorkspaceOwner:RoleConstants.WorkspaceMember,
                Members = w.Members.Select(m => new WorkspaceMemberDto
                {
                    Email = m.User.Email,
                    UserId = m.UserId,
                    Username = m.User.Username,
                    AvatarUrl = m.User.AvatarUrl,
                    RoleId = m.RoleId,
                    RoleName = m.Role.Name ?? RoleConstants.WorkspaceMember,
                    JoinAt = m.JoinAt,
                }).ToList(),
                Projects = w.Projects.Select(p => new WorkspaceProjectDto
                {
                    Id = p.Id,
                    Description = p.Description,
                    Name = p.Name,
                    CreatedAt = p.CreatedAt,
                    MembersCount = p.ProjectMembers.Count,
                    Members = p.ProjectMembers.Select(pm => new ProjectMemberAvatarDto
                    {
                        UserId = pm.UserId,
                        Username = pm.User.Username,
                        AvatarUrl = pm.User.AvatarUrl,
                    }).ToList(),
                    ProjectKey = p.ProjectKey,
                    TasksCount = p.Boards.SelectMany(b => b.Columns).SelectMany(col => col.Cards).Count(ca => !ca.IsDeleted),
                }).OrderByDescending(p => p.CreatedAt).ToList()
            }).FirstOrDefaultAsync(w => w.Id == request.WorkspaceId,cancellationToken) ?? throw new NotFoundException("Workspace không tồn tại");
            return res ;
        }
        private async System.Threading.Tasks.Task GenerateNewUniqueSlugtoUpdate(Workspace workspace, string? requestedname, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(requestedname) &&!string.Equals(workspace.Name,requestedname,StringComparison.OrdinalIgnoreCase))
            {
                var normalizeName = requestedname.Trim();
                var baseSlug = SlugHelper.GenerateSlug(normalizeName);
                var newSlug = baseSlug;
                var existingSlug = await _context.Workspaces
                    .Where(w => (w.Slug.StartsWith(baseSlug + "-") || w.Slug == baseSlug) && w.Id != workspace.Id)
                    .Select(w => w.Slug)
                    .ToHashSetAsync(cancellationToken);
                var count = 1;
                while (existingSlug.Contains(newSlug))
                {
                    newSlug = $"{baseSlug}-{count}";
                    count++;
                }
                workspace.Name = normalizeName;
                workspace.Slug = newSlug;
            }
        }
        private async System.Threading.Tasks.Task UploadImagesAsync(Workspace workspace, UpdateWorkspaceCommand request,CancellationToken cancellationToken)
        {
            if (request.Logo is { Length: > 0 })
            {
                workspace.LogoUrl =
                    await _cloudinaryService.UploadWorkspaceImageAsync(request.Logo,request.WorkspaceId.ToString(),"logo",cancellationToken);
            }

            if (request.CoverImage is { Length: > 0 })
            {
                workspace.CoverImgUrl =
                    await _cloudinaryService.UploadWorkspaceImageAsync(request.CoverImage,request.WorkspaceId.ToString(),"cover",cancellationToken);
            }
        }
    }
}
