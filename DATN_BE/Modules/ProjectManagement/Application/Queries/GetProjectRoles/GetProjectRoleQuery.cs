
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectRoles
{
    public class GetProjectRoleQuery : IRequest<List<ProjectRoleDto>>
    {
        
    }
}