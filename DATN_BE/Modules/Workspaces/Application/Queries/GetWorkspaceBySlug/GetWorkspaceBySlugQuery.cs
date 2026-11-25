using DATN_BE.Modules.Workspaces.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Workspaces.Application.Queries.GetWorkspaceBySlug
{
    public class GetWorkspaceBySlugQuery : IRequest<WorkspaceDetailDto>
    {
        public string Slug { get; set; } = string.Empty;
    }
}
