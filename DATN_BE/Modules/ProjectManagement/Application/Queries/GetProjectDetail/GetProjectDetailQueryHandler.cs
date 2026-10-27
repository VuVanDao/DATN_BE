using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly ICurrentUserService _currentUserService;

        public GetProjectDetailQueryHandler(
            IApplicationDbContext context,
            IProjectAuthorizationService authorizationService,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _authorizationService = authorizationService;
            _currentUserService = currentUserService;
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

            var currentUserId = _currentUserService.UserId;
            var currentMember = currentUserId.HasValue
                ? await _context.ProjectMembers
                    .AsNoTracking()
                    .Include(pm => pm.Role)
                    .FirstOrDefaultAsync(pm => pm.ProjectId == projectExists.projectId && pm.UserId == currentUserId.Value, cancellationToken)
                : null;

            var cardBelongToProject = await _context.Cards
                .Where(card => card.Column.Board.ProjectId == projectExists.projectId)
                .CountAsync(cancellationToken);

            var projectDto = await _context.Projects
                .AsNoTracking()
                .Where(project => project.ProjectKey == request.projectKey && project.WorkspaceId == request.workspaceId)
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
                    CardCount = cardBelongToProject,
                    CreatedAt = project.CreatedAt,
                })
                .FirstAsync(cancellationToken);

            projectDto.IsMember = currentMember != null;
            projectDto.CurrentUserRole = currentMember?.Role?.Name;

            return projectDto;
        }
    }
}
