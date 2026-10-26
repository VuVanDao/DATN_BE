using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectDetail
{
    public sealed class GetProjectDetailQueryHandler : IRequestHandler<GetProjectDetailQuery, ProjectDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;

        public GetProjectDetailQueryHandler(
            IApplicationDbContext context,
            IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }

        public async Task<ProjectDto> Handle(
            GetProjectDetailQuery request,
            CancellationToken cancellationToken)
        {
            var projectExists = await _context.Projects
                .AsNoTracking()
                .Where(p => p.ProjectKey == request.projectKey && p.WorkspaceId == request.workspaceId)
                .Select(p => new
                {
                    projectId = p.Id
                })
                .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Project", request.projectKey);

            await _authorizationService.EnsureCanViewProjectAsync(projectExists.projectId, cancellationToken);

            var CardBelongToProject = await _context.Cards.Where(card => card.Column.Board.ProjectId == projectExists.projectId).CountAsync();
            return await _context.Projects
                .AsNoTracking()
                .Where(project => project.ProjectKey == request.projectKey)
                .Select(project => new ProjectDto
                {
                    Id = project.Id,
                    Name = project.Name,
                    ProjectKey = project.ProjectKey,
                    Description = project.Description,
                    WorkspaceId = project.WorkspaceId,
                    CreatedById = project.CreatedById,
                    CreatedByName = project.CreatedBy.Username,
                    BoardsCount = project.Boards.Count,
                    MembersCount = project.ProjectMembers.Count,
                    SprintCount = project.Sprints.Count,
                    CardCount =  CardBelongToProject,
                    CreatedAt = project.CreatedAt,
                })
                .FirstAsync(cancellationToken);
        }
    }
}
