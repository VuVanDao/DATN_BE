using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.CreateSprint
{
    public class CreateSprintCommandHandler : IRequestHandler<CreateSprintCommand, SprintDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectAuthorizationService _authorizationService;

        public CreateSprintCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _authorizationService = authorizationService;
        }

        public async Task<SprintDto> Handle(CreateSprintCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để tạo Sprint.");

            var projectExists = await _context.Projects
                .AsNoTracking()
                .AnyAsync(p => p.Id == request.ProjectId, cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundException("Project", request.ProjectId);
            }

            await _authorizationService.EnsureCanManageProjectAsync(request.ProjectId, cancellationToken);

            var startDate = request.StartDate ?? DateTimeOffset.UtcNow;
            var endDate = request.EndDate ?? startDate.AddDays(14);

            var sprint = new Sprint
            {
                ProjectId = request.ProjectId,
                Name = request.Name.Trim(),
                Goal = request.Goal?.Trim(),
                StartDate = startDate,
                EndDate = endDate,
                Status = SprintStatus.Future
            };

            _context.Sprints.Add(sprint);
            await _context.SaveChangesAsync(cancellationToken);

            return new SprintDto
            {
                Id = sprint.Id,
                ProjectId = sprint.ProjectId,
                Name = sprint.Name,
                Goal = sprint.Goal,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                Status = sprint.Status,
                CreatedAt = sprint.CreatedAt,
                TotalCards = 0,
                CompletedCards = 0,
                TotalStoryPoints = 0,
                CompletedStoryPoints = 0
            };
        }
    }
}
