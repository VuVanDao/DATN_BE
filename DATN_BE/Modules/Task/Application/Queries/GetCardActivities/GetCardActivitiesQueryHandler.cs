using DATN_BE.Modules.Security.Application.DTOs;
using DATN_BE.Modules.Security.Domain.Enum;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Task.Application.Queries.GetCardActivities;

public class GetCardActivitiesQueryHandler(
    IApplicationDbContext context,
    IProjectAuthorizationService authorizationService)
    : IRequestHandler<GetCardActivitiesQuery, PagedResult<ActivityLogDto>>
{
    public async System.Threading.Tasks.Task<PagedResult<ActivityLogDto>> Handle(GetCardActivitiesQuery request, CancellationToken cancellationToken)
    {
        var card = await context.Cards.IgnoreQueryFilters().AsNoTracking()
            .Include(c => c.Column).ThenInclude(col => col.Board)
            .FirstOrDefaultAsync(c => c.Id == request.CardId, cancellationToken)
            ?? throw new NotFoundException("Card", request.CardId);

        await authorizationService.EnsureCanViewProjectAsync(card.Column.Board.ProjectId, cancellationToken);

        var query = context.ActivityLogs.AsNoTracking()
            .Where(log => log.EntityType == ActivityEntityType.Card && log.EntityId == request.CardId);

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
