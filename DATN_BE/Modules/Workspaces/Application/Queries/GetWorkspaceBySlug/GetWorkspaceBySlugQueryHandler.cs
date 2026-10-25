using DATN_BE.Modules.Workspaces.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Workspaces.Application.Queries.GetWorkspaceBySlug
{
    public class GetWorkspaceBySlugQueryHandler : IRequestHandler<GetWorkspaceBySlugQuery, WorkspaceDetailDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public GetWorkspaceBySlugQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        public async Task<WorkspaceDetailDto> Handle(GetWorkspaceBySlugQuery request, CancellationToken cancellationToken)
        {
            var currUserId = _currentUserService.UserId ?? throw new ForbiddenException("Bạn cần đăng nhập để xem thông tin Workspace");
            var slug = request.Slug.Trim().ToLower();
            return await _context.Workspaces.AsNoTracking()
                .Where(w => w.Slug == slug &&
                    (w.OwnerId == currUserId || w.Members.Any(m => m.UserId == currUserId)))
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
                ?? throw new NotFoundException($"Không tìm thấy Workspace với định danh '{request.Slug}'");
        }
    }
}
