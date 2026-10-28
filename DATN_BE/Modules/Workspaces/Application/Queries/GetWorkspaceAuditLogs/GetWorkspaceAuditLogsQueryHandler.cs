using DATN_BE.Modules.Security.Application.DTOs;
using DATN_BE.Modules.Security.Domain.Enum;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Workspaces.Application.Queries.GetWorkspaceAuditLogs;

public class GetWorkspaceAuditLogsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    : IRequestHandler<GetWorkspaceAuditLogsQuery, PagedResult<ActivityLogDto>>
{
    public async Task<PagedResult<ActivityLogDto>> Handle(GetWorkspaceAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new ForbiddenException("Bạn cần đăng nhập.");
        // Retain access to deletion history; ownership is still checked against persisted data.
        var workspace = await context.Workspaces.IgnoreQueryFilters().AsNoTracking()
            .Where(w => w.Id == request.WorkspaceId)
            .Select(w => new { w.OwnerId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Workspace", request.WorkspaceId);
        if (workspace.OwnerId != userId)
            throw new ForbiddenException("Chỉ chủ sở hữu Workspace mới được xem lịch sử quản trị Workspace.");

        var query = context.ActivityLogs.AsNoTracking()
            .Where(log => log.EntityType == ActivityEntityType.Workspace && log.EntityId == request.WorkspaceId);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(log => log.CreatedAt).ThenByDescending(log => log.Id)
            .Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize)
            .Select(log => new ActivityLogDto
            {
                Id = log.Id, UserId = log.UserId,
                Username = context.Users.IgnoreQueryFilters().Where(u => u.Id == log.UserId).Select(u => u.Username).FirstOrDefault(),
                EntityType = log.EntityType.ToString(), EntityId = log.EntityId,
                ActionType = log.ActionType.ToString(), DetailsJson = log.DetailsJson, CreatedAt = log.CreatedAt
            }).ToListAsync(cancellationToken);
        return new PagedResult<ActivityLogDto>(items, count, request.PageIndex, request.PageSize);
    }
}
