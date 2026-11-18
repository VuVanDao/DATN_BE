using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.MoveCardToSprint
{
    public class MoveCardToSprintCommandHandler : IRequestHandler<MoveCardToSprintCommand, CardDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly IHubContext<BoardHub>? _hubContext;
        private readonly ILogger<MoveCardToSprintCommandHandler>? _logger;

        public MoveCardToSprintCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IProjectAuthorizationService authorizationService,
            IHubContext<BoardHub>? hubContext = null,
            ILogger<MoveCardToSprintCommandHandler>? logger = null)
        {
            _context = context;
            _currentUserService = currentUserService;
            _authorizationService = authorizationService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<CardDto> Handle(MoveCardToSprintCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để thao tác.");

            var card = await _context.Cards
                .Include(c => c.Column)
                    .ThenInclude(col => col.Board)
                .Include(c => c.Assignee)
                .Include(c => c.Reporter)
                .Include(c => c.Comments)
                .FirstOrDefaultAsync(c => c.Id == request.CardId, cancellationToken)
                ?? throw new NotFoundException("Card", request.CardId);

            var projectId = card.Column?.Board?.ProjectId
                ?? throw new NotFoundException("Không tìm thấy Dự án của thẻ này.");

            if (card.Column?.Board?.Type != BoardType.Scrum)
            {
                throw new BadRequestException("Chỉ các thẻ thuộc mô hình Scrum mới có thể đưa vào Sprint hoặc Backlog.");
            }

            // Kiểm tra trạng thái của Sprint nguồn (nếu thẻ đang thuộc 1 Sprint)
            bool isSourceActive = false;
            if (card.SprintId.HasValue)
            {
                var sourceSprint = await _context.Sprints
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == card.SprintId.Value, cancellationToken);
                isSourceActive = sourceSprint?.Status == SprintStatus.Active;
            }

            // Kiểm tra trạng thái của Sprint đích (nếu chuyển vào 1 Sprint)
            bool isTargetActive = false;
            if (request.SprintId.HasValue)
            {
                var targetSprint = await _context.Sprints
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == request.SprintId.Value && s.ProjectId == projectId, cancellationToken)
                    ?? throw new NotFoundException("Sprint", request.SprintId.Value);

                if (targetSprint.BoardId != card.Column!.BoardId)
                {
                    throw new BadRequestException("Không thể đưa thẻ vào Sprint của một Bảng Scrum khác.");
                }

                if (targetSprint.Status == SprintStatus.Closed)
                {
                    throw new BadRequestException("Không thể đưa công việc vào một Sprint đã đóng.");
                }

                isTargetActive = targetSprint.Status == SprintStatus.Active;
            }

            // Nếu thêm/bớt thẻ làm thay đổi phạm vi của Sprint đang chạy -> Bắt buộc là WorkspaceOwner hoặc ProjectLeader!
            if (isSourceActive || isTargetActive)
            {
                await _authorizationService.EnsureCanManageProjectAsync(projectId, cancellationToken);
            }
            else
            {
                await _authorizationService.EnsureCanModifyProjectAsync(projectId, cancellationToken);
            }

            var oldSprintId = card.SprintId;
            var boardId = card.Column!.BoardId;

            card.SprintId = request.SprintId;
            await _context.SaveChangesAsync(cancellationToken);

            var result = new CardDto
            {
                Id = card.Id,
                Title = card.Title,
                Description = card.Description,
                Position = card.Position,
                CoverUrl = card.CoverUrl,
                Type = card.Type,
                Priority = card.Priority,
                StoryPoints = card.StoryPoints,
                StartDate = card.StartDate,
                DueDate = card.DueDate,
                IsCompleted = card.IsCompleted,
                ColumnId = card.ColumnId,
                AssigneeId = card.AssigneeId,
                AssigneeName = card.Assignee?.Username,
                AssigneeAvatarUrl = card.Assignee?.AvatarUrl,
                ReporterId = card.ReporterId,
                ReporterName = card.Reporter?.Username ?? string.Empty,
                ReporterAvatarUrl = card.Reporter?.AvatarUrl,
                SprintId = card.SprintId,
                CreatedAt = card.CreatedAt,
                CommentsCount = card.Comments.Count
            };

            if (_hubContext != null)
            {
                try
                {
                    await _hubContext.Clients
                        .Group(BoardHub.GroupName(boardId))
                        .SendAsync(
                            BoardHub.CardSprintChanged,
                            new CardSprintChangedMessage(
                                boardId,
                                result,
                                oldSprintId,
                                card.SprintId,
                                currentUserId,
                                DateTimeOffset.UtcNow),
                            CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger?.LogWarning(exception, "Không thể phát sự kiện CardSprintChanged cho board {BoardId}.", boardId);
                }
            }

            return result;
        }
    }
}
