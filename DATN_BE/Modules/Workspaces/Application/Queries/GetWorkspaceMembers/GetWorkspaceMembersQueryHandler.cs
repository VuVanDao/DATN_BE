using DATN_BE.Modules.Workspaces.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Workspaces.Application.Queries.GetWorkspaceMembers;

public class GetWorkspaceMembersQueryHandler(IApplicationDbContext _context, ICurrentUserService _currentUserService)
    : IRequestHandler<GetWorkspaceMembersQuery, PagedResult<WorkspaceMemberDto>>
{
    public async Task<PagedResult<WorkspaceMemberDto>> Handle(GetWorkspaceMembersQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new ForbiddenException("Bạn cần đăng nhập.");
        if (!await _context.Workspaces.AnyAsync(w => w.Id == request.WorkspaceId &&
            (w.OwnerId == userId || w.Members.Any(m => m.UserId == userId)), cancellationToken))
            throw new NotFoundException("Không tìm thấy Workspace hoặc bạn không có quyền xem.");

        var query = _context.WorkspaceMembers.Where(m => m.WorkspaceId == request.WorkspaceId).AsNoTracking();
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(p => p.JoinAt).ThenBy(p => p.UserId)
            .Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize)
            .Select(p => new WorkspaceMemberDto
            {
                UserId = p.UserId, Username = p.User.Username ?? string.Empty,
                Email = p.User.Email ?? string.Empty, AvatarUrl = p.User.AvatarUrl,
                RoleId = p.RoleId, RoleName = p.Role.Name, JoinAt = p.JoinAt
            }).ToListAsync(cancellationToken);
        return new PagedResult<WorkspaceMemberDto>(items, count, request.PageIndex, request.PageSize);
    }
}
