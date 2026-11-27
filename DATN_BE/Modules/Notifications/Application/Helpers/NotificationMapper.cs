using DATN_BE.Modules.Notifications.Application.DTOs;
using DATN_BE.Modules.Notifications.Domain.Entities;
using DATN_BE.Modules.Notifications.Domain.Enum;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Enum;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Notifications.Application.Helpers
{
    public static class NotificationMapper
    {
        public static async Task<Dictionary<(EntityType Type, Guid Id), string>> ResolveTargetUrlsAsync(
            IApplicationDbContext context,
            IReadOnlyCollection<Notification> notifications,
            CancellationToken cancellationToken = default)
        {
            var urls = new Dictionary<(EntityType Type, Guid Id), string>();
            if (notifications.Count == 0)
            {
                return urls;
            }

            var workspaceIds = notifications
                .Where(n => n.EntityType == EntityType.Workspace && n.EntityId.HasValue)
                .Select(n => n.EntityId!.Value)
                .Distinct()
                .ToList();

            if (workspaceIds.Count > 0)
            {
                var workspaces = await context.Workspaces
                    .AsNoTracking()
                    .Where(w => workspaceIds.Contains(w.Id))
                    .Select(w => new { w.Id, w.Slug })
                    .ToListAsync(cancellationToken);

                foreach (var ws in workspaces)
                {
                    if (!string.IsNullOrWhiteSpace(ws.Slug))
                    {
                        urls[(EntityType.Workspace, ws.Id)] = $"/workspaces/{ws.Slug}";
                    }
                }
            }

            var projectIds = notifications
                .Where(n => n.EntityType == EntityType.Project && n.EntityId.HasValue)
                .Select(n => n.EntityId!.Value)
                .Distinct()
                .ToList();

            if (projectIds.Count > 0)
            {
                var projects = await context.Projects
                    .AsNoTracking()
                    .Where(p => projectIds.Contains(p.Id))
                    .Select(p => new { p.Id, p.WorkspaceId, p.ProjectKey })
                    .ToListAsync(cancellationToken);

                foreach (var project in projects)
                {
                    if (!string.IsNullOrWhiteSpace(project.ProjectKey))
                    {
                        urls[(EntityType.Project, project.Id)] =
                            $"/workspaces/{project.WorkspaceId}/projects/{project.ProjectKey}";
                    }
                }
            }

            var boardIds = notifications
                .Where(n => n.EntityType == EntityType.Board && n.EntityId.HasValue)
                .Select(n => n.EntityId!.Value)
                .Distinct()
                .ToList();

            if (boardIds.Count > 0)
            {
                var boards = await context.Boards
                    .AsNoTracking()
                    .Where(b => boardIds.Contains(b.Id))
                    .Select(b => new { b.Id, b.Slug })
                    .ToListAsync(cancellationToken);

                foreach (var board in boards)
                {
                    if (!string.IsNullOrWhiteSpace(board.Slug))
                    {
                        urls[(EntityType.Board, board.Id)] = $"/boards/{board.Slug}";
                    }
                }
            }

            var cardIds = notifications
                .Where(n => n.EntityType == EntityType.Card && n.EntityId.HasValue)
                .Select(n => n.EntityId!.Value)
                .Distinct()
                .ToList();

            if (cardIds.Count > 0)
            {
                var cards = await context.Cards
                    .AsNoTracking()
                    .Where(c => cardIds.Contains(c.Id) && !c.Column.IsDeleted && !c.Column.Board.IsDeleted)
                    .Select(c => new
                    {
                        c.Id,
                        BoardSlug = c.Column.Board.Slug,
                        ProjectId = c.Column.Board.ProjectId
                    })
                    .ToListAsync(cancellationToken);

                foreach (var card in cards)
                {
                    if (!string.IsNullOrWhiteSpace(card.BoardSlug))
                    {
                        urls[(EntityType.Card, card.Id)] = BuildCardTargetUrl(card.BoardSlug, card.ProjectId, card.Id);
                    }
                }
            }

            return urls;
        }

        public static string BuildCardTargetUrl(string boardSlug, Guid projectId, Guid cardId)
            => $"/boards/{boardSlug}?projectId={projectId}&cardId={cardId}";

        public static NotificationDispatchItem BuildCardAssignedItem(
            Guid receiverId,
            Guid senderId,
            string? senderName,
            string? senderAvatarUrl,
            Guid cardId,
            string cardTitle,
            string boardSlug,
            Guid projectId)
        {
            var actorName = string.IsNullOrWhiteSpace(senderName) ? "Thành viên" : senderName;
            var targetUrl = !string.IsNullOrWhiteSpace(boardSlug)
                ? BuildCardTargetUrl(boardSlug, projectId, cardId)
                : null;

            return new NotificationDispatchItem(
                ReceiverId: receiverId,
                SenderId: senderId,
                SenderName: actorName,
                SenderAvatarUrl: senderAvatarUrl,
                Type: NotificationType.CardAssigned,
                Title: "Bạn được giao công việc mới",
                Content: $"{actorName} đã giao thẻ \"{cardTitle}\" cho bạn",
                EntityType: EntityType.Card,
                EntityId: cardId,
                TargetUrl: targetUrl);
        }

        public static NotificationDispatchItem BuildDueDateReminderItem(
            Guid receiverId,
            Guid cardId,
            string cardTitle,
            string boardSlug,
            Guid projectId,
            bool isOverdue,
            DateTimeOffset dueDate,
            bool isUnassigned = false,
            int diffDays = 0)
        {
            var targetUrl = !string.IsNullOrWhiteSpace(boardSlug)
                ? BuildCardTargetUrl(boardSlug, projectId, cardId)
                : null;

            var localOffset = TimeSpan.FromHours(7);
            var localDue = dueDate.ToOffset(localOffset);
            var dueFormatted = localDue.ToString("dd/MM/yyyy HH:mm");

            string title;
            string content;

            if (isUnassigned)
            {
                if (isOverdue)
                {
                    var overdueDays = Math.Abs(diffDays);
                    title = "Công việc chưa phân công đã quá hạn!";
                    content = overdueDays > 0
                        ? $"Thẻ \"{cardTitle}\" chưa được gán người làm và đã quá hạn {overdueDays} ngày ({dueFormatted}). Vui lòng phân công hoặc kiểm tra tiến độ."
                        : $"Thẻ \"{cardTitle}\" chưa được gán người làm và đã quá hạn ({dueFormatted}). Vui lòng phân công hoặc kiểm tra tiến độ.";
                }
                else if (diffDays == 0)
                {
                    title = "Nhắc nhở: Công việc chưa phân công sắp đến hạn";
                    content = $"Thẻ \"{cardTitle}\" chưa được gán người làm và đến hạn hôm nay ({dueFormatted}). Vui lòng kiểm tra và phân công xử lý.";
                }
                else
                {
                    title = "Nhắc nhở: Công việc chưa phân công sắp đến hạn";
                    content = $"Thẻ \"{cardTitle}\" chưa được gán người làm và sẽ đến hạn vào {dueFormatted}. Vui lòng kiểm tra và phân công xử lý.";
                }
            }
            else
            {
                if (isOverdue)
                {
                    var overdueDays = Math.Abs(diffDays);
                    title = "Công việc đã quá hạn!";
                    content = overdueDays > 0
                        ? $"Thẻ \"{cardTitle}\" đã quá hạn {overdueDays} ngày ({dueFormatted})."
                        : $"Thẻ \"{cardTitle}\" đã quá hạn ({dueFormatted}).";
                }
                else if (diffDays == 0)
                {
                    title = "Nhắc nhở: Công việc sắp đến hạn";
                    content = $"Thẻ \"{cardTitle}\" đến hạn vào hôm nay ({dueFormatted}). Vui lòng kiểm tra tiến độ.";
                }
                else
                {
                    title = "Nhắc nhở: Công việc sắp đến hạn";
                    content = $"Thẻ \"{cardTitle}\" sẽ đến hạn vào {dueFormatted}. Vui lòng kiểm tra tiến độ.";
                }
            }

            return new NotificationDispatchItem(
                ReceiverId: receiverId,
                SenderId: null,
                SenderName: "Hệ thống",
                SenderAvatarUrl: null,
                Type: NotificationType.DueDateReminder,
                Title: title,
                Content: content,
                EntityType: EntityType.Card,
                EntityId: cardId,
                TargetUrl: targetUrl);
        }

        public static NotificationDto ToDto(
            Notification entity,
            IReadOnlyDictionary<(EntityType Type, Guid Id), string>? resolvedUrls = null,
            string? targetUrlOverride = null,
            string? senderNameOverride = null,
            string? senderAvatarUrlOverride = null)
        {
            if (targetUrlOverride == null && entity.EntityType.HasValue && entity.EntityId.HasValue && resolvedUrls != null)
            {
                resolvedUrls.TryGetValue((entity.EntityType.Value, entity.EntityId.Value), out targetUrlOverride);
            }

            return new NotificationDto
            {
                Id = entity.Id,
                ReceiverId = entity.ReceiverId,
                SenderId = entity.SenderId,
                SenderName = entity.Sender?.Username ?? senderNameOverride,
                SenderAvatarUrl = entity.Sender?.AvatarUrl ?? senderAvatarUrlOverride,
                Title = entity.Title,
                Content = entity.Content,
                Type = entity.Type.ToString(),
                EntityType = entity.EntityType?.ToString(),
                EntityId = entity.EntityId,
                TargetUrl = targetUrlOverride,
                IsRead = entity.IsRead,
                ReadAt = entity.ReadAt,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
