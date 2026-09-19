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
            var workspace = await _context.Workspaces.AsNoTracking()
                .Include(w => w.Owner)
                .Include(w => w.Members).ThenInclude(mem => mem.User)
                .Include(w => w.Members).ThenInclude(mem => mem.Role)
                .Include(w => w.Projects).ThenInclude(proj => proj.ProjectMembers).ThenInclude(projMem => projMem.User)
                .Include(w => w.Projects).ThenInclude(proj => proj.Boards).ThenInclude(b => b.Columns).ThenInclude(col => col.Cards)
                .FirstOrDefaultAsync(w => w.Slug.ToLower().Trim() == request.Slug.ToLower().Trim(),cancellationToken) 
                    ?? throw new NotFoundException($"Không tìm thấy Workspace với định danh '{request.Slug}'.");
            // kiểm tra xem user có thuwojc workspace đó hay không (workspaceOwner or member)
            var isMember = workspace.OwnerId == currUserId || workspace.Members.Any(mem =>  mem.UserId == currUserId);
            if (!isMember)
            {
                throw new ForbiddenException("Bạn không có quyền truy cập vào Workspace này.");
            }
            var myMembership = workspace.Members.FirstOrDefault(mem => mem.UserId == currUserId);
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
