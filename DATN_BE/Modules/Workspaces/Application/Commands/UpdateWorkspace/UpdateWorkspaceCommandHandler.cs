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
            return await _context.Workspaces.AsNoTracking()
                .Where(w => w.Id == request.WorkspaceId)
                .Select(w => new WorkspaceDetailDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    Slug = w.Slug,
                    Description = w.Description,
                    LogoUrl = w.LogoUrl,
                    CoverImageUrl = w.CoverImgUrl,
                    OwnerId = w.OwnerId,
                    OwnerName = w.Owner.Username ?? string.Empty,
                    CreatedAt = w.CreatedAt,
                    CurrentUserRole = w.OwnerId == currUserId ? RoleConstants.WorkspaceOwner :
                        w.Members.Where(m => m.UserId == currUserId).Select(m => m.Role.Name).FirstOrDefault(),
                    MembersCount = w.Members.Count,
                    MemberPreview = w.Members.OrderBy(m => m.JoinAt).ThenBy(m => m.UserId).Take(4)
                        .Select(m => new ProjectMemberAvatarDto
                        {
                            UserId = m.UserId,
                            Username = m.User.Username ?? string.Empty,
                            AvatarUrl = m.User.AvatarUrl
                        }).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Workspace không tồn tại");
        }
        private async System.Threading.Tasks.Task GenerateNewUniqueSlugtoUpdate(Workspace workspace, string? requestedname, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(requestedname) && !string.Equals(workspace.Name, requestedname, StringComparison.OrdinalIgnoreCase))
            {
                var normalizeName = requestedname.Trim();
                var baseSlug = SlugHelper.GenerateSlug(normalizeName);
                if (string.IsNullOrWhiteSpace(baseSlug))
                {
                    baseSlug = "workspace";
                }
                if (baseSlug.Length > 100)
                {
                    baseSlug = baseSlug[..100].TrimEnd('-');
                }

                var existingSlug = await _context.Workspaces
                    .Where(w => (w.Slug == baseSlug || w.Slug.StartsWith(baseSlug + "-")) && w.Id != workspace.Id)
                    .Select(w => w.Slug)
                    .ToHashSetAsync(cancellationToken);

                var newSlug = baseSlug;
                while (existingSlug.Contains(newSlug))
                {
                    var shortCode = SlugHelper.GenerateShortCode(6);
                    newSlug = $"{baseSlug}-{shortCode}";
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
