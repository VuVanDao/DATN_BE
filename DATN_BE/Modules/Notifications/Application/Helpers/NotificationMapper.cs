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
            DateTimeOffset dueDate)
        {
            var targetUrl = !string.IsNullOrWhiteSpace(boardSlug)
                ? BuildCardTargetUrl(boardSlug, projectId, cardId)
                : null;

            var title = isOverdue ? "Công việc đã quá hạn!" : "Nhắc nhở: Công việc sắp đến hạn";
            var content = isOverdue
                ? $"Thẻ \"{cardTitle}\" đã quá hạn ({dueDate.ToLocalTime():dd/MM/yyyy HH:mm})."
                : $"Thẻ \"{cardTitle}\" sẽ đến hạn vào {dueDate.ToLocalTime():dd/MM/yyyy HH:mm}. Vui lòng kiểm tra tiến độ.";

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
            IReadOnlyDictionary<(EntityType Type, Guid Id), string>? targetUrls = null,
            string? targetUrlOverride = null,
            string? senderNameOverride = null,
            string? senderAvatarUrlOverride = null)
        {
            string? resolvedUrl = targetUrlOverride;
            if (resolvedUrl == null && entity.EntityType.HasValue && entity.EntityId.HasValue && targetUrls != null)
            {
                targetUrls.TryGetValue((entity.EntityType.Value, entity.EntityId.Value), out resolvedUrl);
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
                TargetUrl = resolvedUrl,
                IsRead = entity.IsRead,
                ReadAt = entity.ReadAt,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
