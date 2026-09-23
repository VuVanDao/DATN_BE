using DATN_BE.Modules.Workspaces.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
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
            var dtoReturned = await _context.Workspaces
                                .AsNoTracking()
                                .Where(w => w.Slug == slug && w.IsDeleted == false && (w.OwnerId == currUserId || w.Members.Any(mem => mem.UserId == currUserId)))
                                .Select(w => new WorkspaceDetailDto
                                {
                                    Id = w.Id,
                                    Slug = w.Slug,
                                    Description = w.Description,
                                    Name = w.Name,
                                    LogoUrl = w.LogoUrl,
                                    CoverImageUrl = w.CoverImgUrl,
                                    OwnerId = w.OwnerId,
                                    OwnerName = w.Owner.Username ?? string.Empty,
                                    CurrentUserRole = w.OwnerId == currUserId ? "WorkspaceOwner" : (w.Members.Where(mem => mem.UserId == currUserId).Select(mem => mem.Role.Name).FirstOrDefault() ?? "WorkspaceMember"),
                                    CreatedAt = w.CreatedAt,
                                    Members = w.Members.Select(mem => new WorkspaceMemberDto
                                    {
                                        UserId = mem.UserId,
                                        Username = mem.User.Username ?? string.Empty,
                                        Email = mem.User.Email ?? string.Empty,
                                        AvatarUrl = mem.User.AvatarUrl,
                                        RoleId = mem.RoleId,
                                        RoleName = mem.Role.Name ?? string.Empty,
                                        JoinAt = mem.JoinAt
                                    }).ToList(),
                                    Projects = w.Projects.Select(p => new WorkspaceProjectDto
                                    {
                                        Id = p.Id,
                                        Name = p.Name,
                                        ProjectKey = p.ProjectKey,
                                        Description = p.Description,
                                        MembersCount = p.ProjectMembers.Count,
                                        TasksCount = p.Boards.SelectMany(b => b.Columns).SelectMany(col => col.Cards).Count(card => card.IsDeleted == false),
                                        Members = p.ProjectMembers.Select(pm => new ProjectMemberAvatarDto
                                        {
                                            UserId = pm.UserId,
                                            Username = pm.User.Username ?? string.Empty,
                                            AvatarUrl = pm.User.AvatarUrl
                                        }).ToList(),
                                        CreatedAt = w.CreatedAt
                                    }).ToList()
                                }).FirstOrDefaultAsync(cancellationToken)
                                ?? throw new NotFoundException($"Không tìm thấy Workspace với định danh '{request.Slug}'");
            return dtoReturned;
        }
    }
}
