using DATN_BE.Modules.Workspaces.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Workspaces.Application.Queries.GetWorkspaceProjects;

public class GetWorkspaceProjectsQueryHandler(IApplicationDbContext _context, ICurrentUserService _currentUserService)
    : IRequestHandler<GetWorkspaceProjectsQuery, PagedResult<WorkspaceProjectDto>>
{
    public async Task<PagedResult<WorkspaceProjectDto>> Handle(GetWorkspaceProjectsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new ForbiddenException("Bạn cần đăng nhập.");
        if (!await _context.Workspaces.AnyAsync(w => w.Id == request.WorkspaceId &&
            (w.OwnerId == userId || w.Members.Any(m => m.UserId == userId)), cancellationToken))
            throw new NotFoundException("Không tìm thấy Workspace hoặc bạn không có quyền xem.");

        var query = _context.Projects.Where(p => p.WorkspaceId == request.WorkspaceId).AsNoTracking();
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
            .Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize)
            .Select(p => new WorkspaceProjectDto
            {
                Id = p.Id, Name = p.Name, ProjectKey = p.ProjectKey, Description = p.Description,
                CreatedAt = p.CreatedAt, MembersCount = p.ProjectMembers.Count,
                TasksCount = p.Boards.SelectMany(b => b.Columns).SelectMany(c => c.Cards).Count(),
                Members = p.ProjectMembers.OrderBy(m => m.JoinedAt).ThenBy(m => m.UserId).Take(3)
                    .Select(m => new ProjectMemberAvatarDto
                    {
                        UserId = m.UserId, Username = m.User.Username ?? string.Empty, AvatarUrl = m.User.AvatarUrl
                    }).ToList()
            }).ToListAsync(cancellationToken);
        return new PagedResult<WorkspaceProjectDto>(items, count, request.PageIndex, request.PageSize);
    }
}
