using DATN_BE.Modules.Workspaces.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Workspaces.Application.Queries.GetUserWorkspaces
{
    public class GetUserWorkspacesQueryHandler : IRequestHandler<GetUserWorkspacesQuery, List<WorkspaceDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public GetUserWorkspacesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        public async Task<List<WorkspaceDto>> Handle(GetUserWorkspacesQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId ?? throw new ForbiddenException("Bạn cần đăng nhập để xem danh sách Workspace");
            // Lấy tất cả Workspace mà user đang làm Owner HOẶC làm Member
            // 1. Dùng .AsNoTracking(): Tắt hoàn toàn bộ theo dõi của EF Core giúp truy vấn nhanh gấp 2-3 lần!
            var workspace = await _context.Workspaces.AsNoTracking().Where(w => !w.IsDeleted && (w.OwnerId == currentUserId || w.Members.Any(m => m.UserId == currentUserId)))
                .Include(w => w.Owner)
                .Include(w => w.Members).ThenInclude(m => m.Role)
                .Include(w => w.Projects)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync(cancellationToken);
            return workspace.Select(w =>
            {
                var myMembership = w.Members.FirstOrDefault(m => m.UserId == currentUserId);
                var myRole = w.OwnerId == currentUserId ? "WorkspaceOwner" : myMembership?.Role?.Name ?? "Member";

                return new WorkspaceDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    Slug = w.Slug,
                    Description = w.Description,
                    LogoUrl = w.LogoUrl,
                    OwnerId = w.OwnerId,
                    OwnerName = w.Owner?.Username ?? string.Empty,
                    MembersCount = w.Members.Count,
                    ProjectsCount = w.Projects.Count(p => !p.IsDeleted),
                    CurrentUserRole = myRole,
                    CreatedAt = w.CreatedAt
                };
            }).ToList();
        }
    }
}
