using DATN_BE.Modules.Security.Application.DTOs;
using DATN_BE.Modules.Security.Domain.Enum;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectAuditLogs;

public class GetProjectAuditLogsQueryHandler(IApplicationDbContext context, IProjectAuthorizationService authorizationService)
    : IRequestHandler<GetProjectAuditLogsQuery, PagedResult<ActivityLogDto>>
{
    public async Task<PagedResult<ActivityLogDto>> Handle(GetProjectAuditLogsQuery request, CancellationToken cancellationToken)
    {
        if (!await context.Projects.IgnoreQueryFilters().AnyAsync(p => p.Id == request.ProjectId, cancellationToken))
            throw new NotFoundException("Project", request.ProjectId);
        await authorizationService.EnsureCanViewProjectAuditAsync(request.ProjectId, cancellationToken);

        var query = context.ActivityLogs.AsNoTracking()
            .Where(log => log.EntityType == ActivityEntityType.Project && log.EntityId == request.ProjectId);
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
