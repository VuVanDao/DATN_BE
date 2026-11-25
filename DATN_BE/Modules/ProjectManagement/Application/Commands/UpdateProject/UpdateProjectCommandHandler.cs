using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateProject
{
    public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly ICurrentUserService _currentUserService;

        public UpdateProjectCommandHandler(
            IApplicationDbContext context,
            IProjectAuthorizationService authorizationService,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _authorizationService = authorizationService;
            _currentUserService = currentUserService;
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

            var currentUserId = _currentUserService.UserId;
            var currentMember = currentUserId.HasValue
                ? await _context.ProjectMembers
                    .AsNoTracking()
                    .Include(pm => pm.Role)
                    .FirstOrDefaultAsync(pm => pm.ProjectId == project.Id && pm.UserId == currentUserId.Value, cancellationToken)
                : null;

            var result = await _context.Projects
                .AsNoTracking()
                .Where(p => p.Id == request.ProjectId)
                .Select(p => new ProjectDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    ProjectKey = p.ProjectKey,
                    Description = p.Description,
                    WorkspaceId = p.WorkspaceId,
                    CreatedById = p.CreatedById,
                    CreatedByName = p.CreatedBy.Username,
                    BoardsCount = p.Boards.Count,
                    MembersCount = p.ProjectMembers.Count,
                    SprintCount = p.Sprints.Count(s => !s.IsDeleted && s.Status != SprintStatus.Closed),
                    CardCount = _context.Cards.Count(card => card.Column.Board.ProjectId == p.Id),
                    CreatedAt = p.CreatedAt
                })
                .FirstAsync(cancellationToken);

            var isWorkspaceOwner = currentUserId.HasValue &&
                await _context.Workspaces.AnyAsync(w => w.Id == project.WorkspaceId && w.OwnerId == currentUserId.Value, cancellationToken);

            result.IsMember = isWorkspaceOwner || currentMember != null;
            result.CurrentUserRole = isWorkspaceOwner
                ? (currentMember?.Role?.Name ?? RoleConstants.WorkspaceOwner)
                : currentMember?.Role?.Name;

            return result;
        }
    }
}
