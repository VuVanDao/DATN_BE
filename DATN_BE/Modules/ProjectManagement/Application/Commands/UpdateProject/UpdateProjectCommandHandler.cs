using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateProject
{
    public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;

        public UpdateProjectCommandHandler(
            IApplicationDbContext context,
            IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }

        public async Task<ProjectDto> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(project => project.Id == request.ProjectId, cancellationToken)
                ?? throw new NotFoundException("Project", request.ProjectId);

            await _authorizationService.EnsureCanManageProjectAsync(project.Id, cancellationToken);

            var normalizedKey = request.ProjectKey.Trim().ToUpperInvariant();
            var keyExists = await _context.Projects.AnyAsync(other =>
                other.WorkspaceId == project.WorkspaceId &&
                other.ProjectKey == normalizedKey &&
                other.Id != project.Id, cancellationToken);

            if (keyExists)
            {
                throw new BadRequestException("Mã dự án này đã tồn tại trong workspace này.");
            }

            project.Name = request.Name.Trim();
            project.ProjectKey = normalizedKey;
            project.Description = request.Description;

            await _context.SaveChangesAsync(cancellationToken);

            return await _context.Projects
                .AsNoTracking()
                .Where(project => project.Id == request.ProjectId)
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
                    CardCount = _context.Cards.Count(card => card.Column.Board.ProjectId == project.Id),
                    CreatedAt = project.CreatedAt
                })
                .FirstAsync(cancellationToken);
        }
    }
}
