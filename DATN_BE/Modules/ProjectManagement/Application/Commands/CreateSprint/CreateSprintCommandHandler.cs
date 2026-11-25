using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.CreateSprint
{
    public class CreateSprintCommandHandler : IRequestHandler<CreateSprintCommand, SprintDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly IHubContext<BoardHub>? _hubContext;
        private readonly ILogger<CreateSprintCommandHandler>? _logger;

        public CreateSprintCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IProjectAuthorizationService authorizationService,
            IHubContext<BoardHub>? hubContext = null,
            ILogger<CreateSprintCommandHandler>? logger = null)
        {
            _context = context;
            _currentUserService = currentUserService;
            _authorizationService = authorizationService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<SprintDto> Handle(CreateSprintCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new UnauthorizedException("Bạn cần đăng nhập để tạo Sprint.");

            var projectExists = await _context.Projects
                .AsNoTracking()
                .AnyAsync(p => p.Id == request.ProjectId, cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundException("Project", request.ProjectId);
            }

            await _authorizationService.EnsureCanManageProjectAsync(request.ProjectId, cancellationToken);

            var board = await _context.Boards
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == request.BoardId && b.ProjectId == request.ProjectId, cancellationToken)
                ?? throw new NotFoundException("Bảng Scrum không tồn tại trong Dự án này.");

            if (board.Type != BoardType.Scrum)
            {
                throw new BadRequestException("Chỉ có thể tạo Sprint cho Bảng có loại Scrum.");
            }

            var startDate = request.StartDate ?? DateTimeOffset.UtcNow;
            var endDate = request.EndDate ?? startDate.AddDays(14);

            var sprint = new Sprint
            {
                ProjectId = request.ProjectId,
                BoardId = request.BoardId,
                Name = request.Name.Trim(),
                Goal = request.Goal?.Trim(),
                StartDate = startDate,
                EndDate = endDate,
                Status = SprintStatus.Future
            };

            _context.Sprints.Add(sprint);
            await _context.SaveChangesAsync(cancellationToken);

            var result = new SprintDto
            {
                Id = sprint.Id,
                ProjectId = sprint.ProjectId,
                BoardId = sprint.BoardId,
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

            if (_hubContext != null)
            {
                try
                {
                    await _hubContext.Clients
                        .Group(BoardHub.GroupName(request.BoardId))
                        .SendAsync(
                            BoardHub.SprintCreated,
                            new SprintCreatedMessage(
                                request.BoardId,
                                result,
                                currentUserId,
                                DateTimeOffset.UtcNow),
                            CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger?.LogWarning(exception, "Không thể phát sự kiện SprintCreated cho board {BoardId}.", request.BoardId);
                }
            }

            return result;
        }
    }
}
