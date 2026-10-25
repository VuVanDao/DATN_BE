using DATN_BE.Modules.Workspaces.Application.DTOs;
using DATN_BE.Shared.Application.Models;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetWorkspaceProjects;

public record GetWorkspaceProjectsQuery(Guid WorkspaceId, int PageIndex = 1, int PageSize = 20) : IRequest<PagedResult<WorkspaceProjectDto>>;
