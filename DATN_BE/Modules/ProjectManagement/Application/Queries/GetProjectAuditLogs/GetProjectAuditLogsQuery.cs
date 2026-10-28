using DATN_BE.Modules.Security.Application.DTOs;
using DATN_BE.Shared.Application.Models;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectAuditLogs;

public record GetProjectAuditLogsQuery(Guid ProjectId, int PageIndex = 1, int PageSize = 20)
    : IRequest<PagedResult<ActivityLogDto>>;
