using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Notifications.Application.Helpers;
using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;
using DATN_BE.Modules.Security.Domain.Enum;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Shared.Application.Events;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.Task.Application.Commands.UpdateCard
{
    public class UpdateCardCommandHandler : IRequestHandler<UpdateCardCommand, CardDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly ICloudinaryService? _cloudinaryService;
        private readonly IHubContext<BoardHub>? _hubContext;
        private readonly ICurrentUserService? _currentUserService;
        private readonly ILogger<UpdateCardCommandHandler>? _logger;
        private readonly INotificationService? _notificationService;
        private readonly IMediator? _mediator;

        public UpdateCardCommandHandler(
            IApplicationDbContext context,
            IProjectAuthorizationService authorizationService,
            ICloudinaryService? cloudinaryService = null,
            IHubContext<BoardHub>? hubContext = null,
            ICurrentUserService? currentUserService = null,
            ILogger<UpdateCardCommandHandler>? logger = null,
            INotificationService? notificationService = null,
            IMediator? mediator = null)
        {
            _context = context;
            _authorizationService = authorizationService;
            _cloudinaryService = cloudinaryService;
            _hubContext = hubContext;
            _currentUserService = currentUserService;
            _logger = logger;
            _notificationService = notificationService;
            _mediator = mediator;
        }

        public async Task<CardDto> Handle(UpdateCardCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService?.UserId;
            var targetCard = await _context.Cards.Include(c => c.Assignee).Include(c => c.Reporter)
                .Include(c => c.Column)
                .ThenInclude(c => c.Board)
                .Include(c => c.Comments)
                .Include(c => c.BlockedByDependencies)
                    .ThenInclude(d => d.BlockingCard)
                        .ThenInclude(bc => bc.Column)
                .FirstOrDefaultAsync(c => c.Id == request.CardId, cancellationToken)
                ?? throw new NotFoundException($"Không tìm thấy card với id {request.CardId}");
            await _authorizationService.EnsureCanModifyProjectAsync(targetCard.Column.Board.ProjectId, cancellationToken);

            if (request.StartDate.HasValue && request.DueDate.HasValue && request.DueDate.Value < request.StartDate.Value)
            {
                throw new BadRequestException("Hạn chót (DueDate) phải lớn hơn hoặc bằng Ngày bắt đầu (StartDate).");
            }
            
            if (request.ProgressPercentage == 100)
            {
                var unfinishedBlockers = targetCard.BlockedByDependencies
                    .Where(d => d.BlockingCard != null
                             && !d.BlockingCard.IsDeleted
                             && !((d.BlockingCard.Column != null && d.BlockingCard.Column.IsDoneColumn) || d.BlockingCard.ProgressPercentage == 100))
                    .Select(d => d.BlockingCard.Title)
                    .ToList();

                if (unfinishedBlockers.Count > 0)
                {
                    var names = string.Join(", ", unfinishedBlockers.Take(3).Select(t => $"\"{t}\""));
                    throw new BadRequestException($"Không thể đánh dấu hoàn thành thẻ vì đang bị chặn bởi công việc chưa xong: {names}.");
                }
            }

            var previousTitle = targetCard.Title;
            var previousDueDate = targetCard.DueDate;
            var previousPriority = targetCard.Priority;
            var previousProgress = targetCard.ProgressPercentage;
            var previousAssigneeId = targetCard.AssigneeId;
            var previousAssigneeName = targetCard.Assignee?.Username;

            targetCard.Title = request.Title.Trim();
            targetCard.Description = request.Description;

            // Xử lý ảnh bìa tương tự Workspace: ưu tiên file upload lên Cloudinary, hoặc nhận URL trực tiếp
            if (request.CoverImage is { Length: > 0 })
            {
                if (_cloudinaryService != null)
                {
                    targetCard.CoverUrl = await _cloudinaryService.UploadCardCoverAsync(request.CoverImage, targetCard.Id.ToString(), cancellationToken);
                }
            }
            else if (!string.IsNullOrWhiteSpace(request.CoverUrl) && request.CoverUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                if (_cloudinaryService != null)
                {
                    targetCard.CoverUrl = await _cloudinaryService.UploadCardCoverFromDataUriAsync(request.CoverUrl, targetCard.Id.ToString(), cancellationToken);
                }
            }
            else
            {
                targetCard.CoverUrl = string.IsNullOrWhiteSpace(request.CoverUrl) ? null : request.CoverUrl;
            }

            targetCard.Type = request.Type;
            targetCard.Priority = request.Priority;
            targetCard.StartDate = request.StartDate;
            targetCard.DueDate = request.DueDate;
            targetCard.StoryPoints = request.StoryPoints;
            targetCard.ProgressPercentage = request.ProgressPercentage;

            if (request.SprintId.HasValue)
            {
                targetCard.SprintId = request.SprintId.Value;
            }

            if (request.AssigneeId.HasValue)
            {
                if (targetCard.AssigneeId != request.AssigneeId.Value || targetCard.Assignee == null)
                {
                    var assignee = await _context.Users
                        .FirstOrDefaultAsync(u => u.Id == request.AssigneeId.Value &&
                            u.ProjectMembers.Any(pm => pm.ProjectId == targetCard.Column.Board.ProjectId), cancellationToken)
                        ?? throw new BadRequestException("Người được gán thẻ không tồn tại hoặc không thuộc Project này.");

                    targetCard.AssigneeId = request.AssigneeId;
                    targetCard.Assignee = assignee;
                }
            }
            else
            {
                targetCard.AssigneeId = null;
                targetCard.Assignee = null;
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (_mediator != null && currentUserId.HasValue)
            {
                if (previousAssigneeId != targetCard.AssigneeId)
                {
                    if (targetCard.AssigneeId.HasValue)
                    {
                        await _mediator.Publish(new EntityActivityLoggedEvent(
                            currentUserId.Value,
                            ActivityEntityType.Card,
                            targetCard.Id,
                            ActivityActionType.AssignedUser,
                            new
                            {
                                cardTitle = targetCard.Title,
                                assigneeId = targetCard.AssigneeId,
                                assigneeName = targetCard.Assignee?.Username
                            }), cancellationToken);
                    }
                    else
                    {
                        await _mediator.Publish(new EntityActivityLoggedEvent(
                            currentUserId.Value,
                            ActivityEntityType.Card,
                            targetCard.Id,
                            ActivityActionType.UnassignedUser,
                            new
                            {
                                cardTitle = targetCard.Title,
                                previousAssigneeName
                            }), cancellationToken);
                    }
                }

                if (previousDueDate != targetCard.DueDate)
                {
                    await _mediator.Publish(new EntityActivityLoggedEvent(
                        currentUserId.Value,
                        ActivityEntityType.Card,
                        targetCard.Id,
                        ActivityActionType.UpdatedDueDate,
                        new
                        {
                            cardTitle = targetCard.Title,
                            oldDueDate = previousDueDate,
                            newDueDate = targetCard.DueDate
                        }), cancellationToken);
                }

                if (previousTitle != targetCard.Title ||
                    previousPriority != targetCard.Priority ||
                    previousProgress != targetCard.ProgressPercentage)
                {
                    await _mediator.Publish(new EntityActivityLoggedEvent(
                        currentUserId.Value,
                        ActivityEntityType.Card,
                        targetCard.Id,
                        ActivityActionType.Updated,
                        new
                        {
                            cardTitle = targetCard.Title,
                            priority = targetCard.Priority.ToString(),
                            progressPercentage = targetCard.ProgressPercentage
                        }), cancellationToken);
                }
            }
            
            var result = new CardDto
            {
                Id = targetCard.Id,
                Title = targetCard.Title,
                Description = targetCard.Description,
                Position = targetCard.Position,
                CoverUrl = targetCard.CoverUrl,
                Type = targetCard.Type,
                Priority = targetCard.Priority,
                StoryPoints = targetCard.StoryPoints,
                StartDate = targetCard.StartDate,
                DueDate = targetCard.DueDate,
                ProgressPercentage = targetCard.ProgressPercentage,
                ColumnId = targetCard.ColumnId,
                AssigneeId = targetCard.AssigneeId,
                AssigneeName = targetCard.Assignee?.Username,
                AssigneeAvatarUrl = targetCard.Assignee?.AvatarUrl,
                ReporterId = targetCard.ReporterId,
                ReporterName = targetCard.Reporter?.Username ?? string.Empty,
                ReporterAvatarUrl = targetCard.Reporter?.AvatarUrl,
                SprintId = targetCard.SprintId,
                CreatedAt = targetCard.CreatedAt,
                CommentsCount = targetCard.Comments.Count,
                IsBlocked = targetCard.BlockedByDependencies.Any(d => d.BlockingCard != null && !d.BlockingCard.IsDeleted && !((d.BlockingCard.Column != null && d.BlockingCard.Column.IsDoneColumn) || d.BlockingCard.ProgressPercentage == 100)),
                BlockedByCount = targetCard.BlockedByDependencies.Count(d => d.BlockingCard != null && !d.BlockingCard.IsDeleted && !((d.BlockingCard.Column != null && d.BlockingCard.Column.IsDoneColumn) || d.BlockingCard.ProgressPercentage == 100))
            };

            if (_hubContext != null && targetCard.Column?.Board != null)
            {
                try
                {
                    var boardId = targetCard.Column.BoardId;
                    await _hubContext.Clients
                        .Group(BoardHub.GroupName(boardId))
                        .SendAsync(
                            BoardHub.CardUpdated,
                            new CardUpdatedMessage(
                                boardId,
                                result,
                                _currentUserService?.UserId ?? Guid.Empty,
                                DateTimeOffset.UtcNow),
                            CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger?.LogWarning(exception, "Không thể phát sự kiện CardUpdated cho board {BoardId}.", targetCard.Column.BoardId);
                }
            }

            if (_notificationService != null
                && _currentUserService?.UserId is Guid actorId
                && targetCard.AssigneeId.HasValue
                && targetCard.AssigneeId.Value != previousAssigneeId
                && targetCard.AssigneeId.Value != actorId
                && targetCard.Column?.Board != null)
            {
                string? actorName = _currentUserService.Username;
                string? actorAvatarUrl = null;

                if (actorId == targetCard.ReporterId && targetCard.Reporter != null)
                {
                    actorName = targetCard.Reporter.Username;
                    actorAvatarUrl = targetCard.Reporter.AvatarUrl;
                }
                else
                {
                    var actor = await _context.Users
                        .AsNoTracking()
                        .Where(u => u.Id == actorId)
                        .Select(u => new { u.Username, u.AvatarUrl })
                        .FirstOrDefaultAsync(cancellationToken);

                    if (actor != null)
                    {
                        actorName = actor.Username;
                        actorAvatarUrl = actor.AvatarUrl;
                    }
                }

                var notificationItem = NotificationMapper.BuildCardAssignedItem(
                    receiverId: targetCard.AssigneeId.Value,
                    senderId: actorId,
                    senderName: actorName,
                    senderAvatarUrl: actorAvatarUrl,
                    cardId: targetCard.Id,
                    cardTitle: targetCard.Title,
                    boardSlug: targetCard.Column.Board.Slug,
                    projectId: targetCard.Column.Board.ProjectId);

                await _notificationService.CreateAndSendManyAsync([notificationItem], cancellationToken);
            }

            return result;
        }
    }
}
