using DATN_BE.Modules.Workspaces.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Workspaces.Application.Queries.GetUserWorkspaces
{
    public class GetUserWorkspacesQuery : IRequest<List<WorkspaceDto>>
    {
    }
}
