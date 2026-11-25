using DATN_BE.Modules.Security.Application.DTOs;
using DATN_BE.Shared.Application.Models;
using MediatR;

namespace DATN_BE.Modules.Workspaces.Application.Queries.GetWorkspaceAuditLogs;

public record GetWorkspaceAuditLogsQuery(Guid WorkspaceId, int PageIndex = 1, int PageSize = 20)
    : IRequest<PagedResult<ActivityLogDto>>;
