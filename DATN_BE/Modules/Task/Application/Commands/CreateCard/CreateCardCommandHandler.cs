using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Notifications.Application.Helpers;
using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;
using DATN_BE.Modules.Security.Domain.Enum;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Shared.Application.Events;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.Task.Application.Commands.CreateCard
{
    public class CreateCardCommandHandler : IRequestHandler<CreateCardCommand, CardDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly IHubContext<BoardHub>? _hubContext;
        private readonly ILogger<CreateCardCommandHandler>? _logger;
        private readonly INotificationService? _notificationService;
        private readonly IMediator? _mediator;

        public CreateCardCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IProjectAuthorizationService authorizationService,
            IHubContext<BoardHub>? hubContext = null,
            ILogger<CreateCardCommandHandler>? logger = null,
            INotificationService? notificationService = null,
            IMediator? mediator = null)
        {
            _context = context;
            _currentUserService = currentUserService;
            _authorizationService = authorizationService;
            _hubContext = hubContext;
            _logger = logger;
            _notificationService = notificationService;
            _mediator = mediator;
        }
        public async Task<CardDto> Handle(CreateCardCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new UnauthorizedException("Bạn cần đăng nhập để tạo Thẻ công việc.");
            // Kiểm tra xem cột có tồn tại không
            var targetColumn = await _context.Columns.Include(c => c.Board)
                .FirstOrDefaultAsync(c => c.Id == request.ColumnId, cancellationToken)
                ?? throw new NotFoundException("Cột không tồn tại.");
            if (targetColumn.Board == null)
            {
                throw new NotFoundException("Bảng của cột này không tồn tại hoặc đã bị xoá.");
            }
            await _authorizationService.EnsureCanModifyProjectAsync(targetColumn.Board.ProjectId, cancellationToken);
            // Kiểm tra xem người dùng hiện tại có tồn tại không
            var reporter = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken)
                ?? throw new NotFoundException("Người dùng không tồn tại.");

            User? assignee = null;
            if (request.AssigneeId.HasValue)
            {
                assignee = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.AssigneeId.Value &&
                        u.ProjectMembers.Any(pm => pm.ProjectId == targetColumn.Board.ProjectId), cancellationToken)
                    ?? throw new BadRequestException("Người được gán thẻ không tồn tại hoặc không thuộc Project này.");
            }

            // 1. Tự động tính Position của card tiếp theo trong Cột đó
            var maxPosition = await _context.Cards.Where(c => c.ColumnId == request.ColumnId)
                .MaxAsync(c => (int?)c.Position, cancellationToken) ?? -1;

            // Nếu Bảng thuộc loại Scrum và chưa chỉ định SprintId:
            // Tự động gán SprintId là Sprint đang Active của chính Bảng Scrum này (nếu có)
            Guid? finalSprintId = request.SprintId;
            if (targetColumn.Board.Type == BoardType.Scrum && !finalSprintId.HasValue)
            {
                var activeSprint = await _context.Sprints
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.BoardId == targetColumn.BoardId && s.Status == SprintStatus.Active, cancellationToken);
                if (activeSprint != null)
                {
                    finalSprintId = activeSprint.Id;
                }
            }

            // 2. Tạo Thẻ mới
            var newCard = new Card
            {
                ColumnId = request.ColumnId,
                Title = request.Title.Trim(),
                Description = request.Description,
                Position = maxPosition + 1,
                Type = request.Type,
                Priority = request.Priority,
                AssigneeId = request.AssigneeId,
                ReporterId = currentUserId,
                StartDate = request.StartDate,
                DueDate = request.DueDate,
                StoryPoints = request.StoryPoints,
                SprintId = finalSprintId,
                IsCompleted = targetColumn.IsDoneColumn
            };
            _context.Cards.Add(newCard);
            await _context.SaveChangesAsync(cancellationToken);

            var result = new CardDto
            {
                Id = newCard.Id,
                Title = newCard.Title,
                Description = newCard.Description,
                Position = newCard.Position,
                CoverUrl = newCard.CoverUrl,
                Type = newCard.Type,
                Priority = newCard.Priority,
                StoryPoints = newCard.StoryPoints,
                StartDate = newCard.StartDate,
                DueDate = newCard.DueDate,
                IsCompleted = newCard.IsCompleted,
                ColumnId = newCard.ColumnId,
                AssigneeId = newCard.AssigneeId,
                AssigneeName = assignee?.Username,
                AssigneeAvatarUrl = assignee?.AvatarUrl,
                ReporterId = newCard.ReporterId,
                ReporterName = reporter.Username,
                ReporterAvatarUrl = reporter.AvatarUrl,
                SprintId = newCard.SprintId,
                CreatedAt = newCard.CreatedAt,
                CommentsCount = 0
            };

            if (_hubContext != null)
            {
                try
                {
                    await _hubContext.Clients
                        .Group(BoardHub.GroupName(targetColumn.BoardId))
                        .SendAsync(
                            BoardHub.CardCreated,
                            new CardCreatedMessage(
                                targetColumn.BoardId,
                                result,
                                currentUserId,
                                DateTimeOffset.UtcNow),
                            CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger?.LogWarning(exception, "Không thể phát sự kiện CardCreated cho board {BoardId}.", targetColumn.BoardId);
                }
            }

            if (_notificationService != null && newCard.AssigneeId.HasValue && newCard.AssigneeId.Value != currentUserId)
            {
                var notificationItem = NotificationMapper.BuildCardAssignedItem(
                    receiverId: newCard.AssigneeId.Value,
                    senderId: currentUserId,
                    senderName: reporter.Username,
                    senderAvatarUrl: reporter.AvatarUrl,
                    cardId: newCard.Id,
                    cardTitle: newCard.Title,
                    boardSlug: targetColumn.Board.Slug,
                    projectId: targetColumn.Board.ProjectId);

                await _notificationService.CreateAndSendManyAsync([notificationItem], cancellationToken);
            }

            if (_mediator != null)
            {
                await _mediator.Publish(new EntityActivityLoggedEvent(
                    currentUserId,
                    ActivityEntityType.Card,
                    newCard.Id,
                    ActivityActionType.Created,
                    new
                    {
                        cardTitle = newCard.Title,
                        columnId = targetColumn.Id,
                        columnTitle = targetColumn.Title,
                        boardId = targetColumn.BoardId
                    }), cancellationToken);
            }

            return result;
        }
    }
}
