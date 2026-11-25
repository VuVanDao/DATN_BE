using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetMyProjects
{
    public sealed record GetMyProjectsQuery(Guid? WorkspaceId = null)
        : IRequest<List<MyProjectsWorkspaceDto>>;
}
