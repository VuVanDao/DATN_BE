using DATN_BE.Modules.Security.Application.DTOs;
using DATN_BE.Modules.Security.Domain.Enum;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetColumnActivities;

public class GetColumnActivitiesQueryHandler(
    IApplicationDbContext context,
    IProjectAuthorizationService authorizationService)
    : IRequestHandler<GetColumnActivitiesQuery, PagedResult<ActivityLogDto>>
{
    public async Task<PagedResult<ActivityLogDto>> Handle(GetColumnActivitiesQuery request, CancellationToken cancellationToken)
    {
        var column = await context.Columns.IgnoreQueryFilters().AsNoTracking()
            .Include(c => c.Board)
            .FirstOrDefaultAsync(c => c.Id == request.ColumnId, cancellationToken)
            ?? throw new NotFoundException("Column", request.ColumnId);

        await authorizationService.EnsureCanViewProjectAsync(column.Board.ProjectId, cancellationToken);

        var query = context.ActivityLogs.AsNoTracking()
            .Where(log => log.EntityType == ActivityEntityType.Column && log.EntityId == request.ColumnId);

        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(log => log.CreatedAt).ThenByDescending(log => log.Id)
            .Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize)
            .Select(log => new ActivityLogDto
            {
                Id = log.Id,
                UserId = log.UserId,
                Username = log.User != null ? log.User.Username : context.Users.IgnoreQueryFilters().Where(u => u.Id == log.UserId).Select(u => u.Username).FirstOrDefault(),
                UserAvatarUrl = log.User != null ? log.User.AvatarUrl : context.Users.IgnoreQueryFilters().Where(u => u.Id == log.UserId).Select(u => u.AvatarUrl).FirstOrDefault(),
                EntityType = log.EntityType.ToString(),
                EntityId = log.EntityId,
                ActionType = log.ActionType.ToString(),
                DetailsJson = log.DetailsJson,
                CreatedAt = log.CreatedAt
            }).ToListAsync(cancellationToken);

        return new PagedResult<ActivityLogDto>(items, count, request.PageIndex, request.PageSize);
    }
}
