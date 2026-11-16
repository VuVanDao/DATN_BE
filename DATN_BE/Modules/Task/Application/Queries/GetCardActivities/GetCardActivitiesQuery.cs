using DATN_BE.Modules.Security.Application.DTOs;
using DATN_BE.Shared.Application.Models;
using MediatR;

namespace DATN_BE.Modules.Task.Application.Queries.GetCardActivities;

public record GetCardActivitiesQuery(Guid CardId, int PageIndex = 1, int PageSize = 20)
    : IRequest<PagedResult<ActivityLogDto>>;
