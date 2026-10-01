using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectDetail
{
    public sealed record GetProjectDetailQuery(Guid ProjectId) : IRequest<ProjectDto>;
}
