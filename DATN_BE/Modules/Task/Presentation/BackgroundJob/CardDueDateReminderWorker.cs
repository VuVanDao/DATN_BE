using DATN_BE.Modules.Notifications.Application.Helpers;
using DATN_BE.Modules.Notifications.Domain.Enum;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using DATN_BE.Shared.Enum;
using DATN_BE.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.Task.Presentation.BackgroundJob
{
    /// <summary>
    /// Worker chạy nền định kỳ để quét các thẻ công việc sắp hết hạn (trong 24h) hoặc đã quá hạn,
    /// tự động gửi thông báo (DueDateReminder) tới người thực hiện (Assignee), hoặc tới
    /// ProjectLeader / WorkspaceOwner nếu thẻ chưa được phân công.
    /// </summary>
    public class CardDueDateReminderWorker : BackgroundService
    {
        private static readonly TimeSpan LocalTimeZoneOffset = TimeSpan.FromHours(7);// tạo 1 khoảng thời gian cố định 7 tiếng
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CardDueDateReminderWorker> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

        public CardDueDateReminderWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<CardDueDateReminderWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CardDueDateReminderWorker đã khởi động.");

            // Đợi 10 giây ban đầu để hệ thống và database kết nối ổn định
            try
            {
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendRemindersAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong tiến trình kiểm tra hạn chót thẻ công việc.");
                }

                try
                {
                    await System.Threading.Tasks.Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("CardDueDateReminderWorker đã dừng.");
        }

        public async System.Threading.Tasks.Task CheckAndSendRemindersAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var now = DateTimeOffset.UtcNow;
            var dbQueryStartTime = now.AddDays(-8);
            var dbQueryEndTime = now.AddDays(2);

            // 1. Lấy danh sách thẻ thỏa mãn điều kiện thời gian tổng quát
            var CardsNotCompleteFrom8BeforeTo2DayLater = await db.Cards
                .AsNoTracking()
                .Include(c => c.Column).ThenInclude(col => col.Board)
                .Where(c => !c.IsCompleted
                            && c.DueDate.HasValue
                            && c.DueDate.Value >= dbQueryStartTime
                            && c.DueDate.Value <= dbQueryEndTime
                            && c.Column != null
                            && !c.Column.IsDoneColumn
                            && !c.Column.IsDeleted
                            && !c.Column.Board.IsDeleted)
                .Select(c => new
                {
                    CardId = c.Id,
                    c.Title,
                    c.AssigneeId,
                    DueDate = c.DueDate!.Value,
                    BoardSlug = c.Column.Board.Slug,
                    ProjectId = c.Column.Board.ProjectId
                })
                .ToListAsync(cancellationToken);

            if (CardsNotCompleteFrom8BeforeTo2DayLater.Count == 0)
            {
                return;
            }

            // 2. danh sách
            var todayLocal = now.ToOffset(LocalTimeZoneOffset).Date;

            var CardsExpiredFrom7DayBeforeTo1DayLater = CardsNotCompleteFrom8BeforeTo2DayLater
                .Select(c =>
                {
                    var dueLocal = c.DueDate.ToOffset(LocalTimeZoneOffset).Date;
                    var diffDays = (dueLocal - todayLocal).Days;
                    var isOverdue = diffDays < 0;
                    return new
                    {
                        c.CardId,
                        c.Title,
                        c.AssigneeId,
                        c.DueDate,
                        c.BoardSlug,
                        c.ProjectId,
                        diffDays,
                        isOverdue
                    };
                })
                .Where(c => c.diffDays >= -7 && c.diffDays <= 1)
                .ToList();

            if (CardsExpiredFrom7DayBeforeTo1DayLater.Count == 0)
            {
                return;
            }

            // danh sách thẻ chưa được phân công
            var unassignedCards = CardsExpiredFrom7DayBeforeTo1DayLater.Where(c => !c.AssigneeId.HasValue).ToList();
            // danh sách project của các card chưua phân công
            var unassignedProjectIds = unassignedCards.Select(c => c.ProjectId).Distinct().ToList();

            var projectLeadersMap = new Dictionary<Guid, Guid>();
            var workspaceOwnersMap = new Dictionary<Guid, Guid>();

            if (unassignedProjectIds.Count > 0)
            {
                // tìm tất cả ProjectLeader của các dự án có thẻ chưa gán, nếu không có leader thì fallback sang WorkspaceOwner
                var leaders = await db.ProjectMembers
                    .AsNoTracking()
                    .Where(pm => unassignedProjectIds.Contains(pm.ProjectId) && pm.Role.Name == RoleConstants.ProjectLeader)
                    .Select(pm => new { pm.ProjectId, pm.UserId })
                    .ToListAsync(cancellationToken);

                projectLeadersMap = leaders.ToDictionary(l => l.ProjectId, l => l.UserId);

                var wsOwners = await db.Projects
                    .AsNoTracking()
                    .Where(p => unassignedProjectIds.Contains(p.Id))
                    .Select(p => new { ProjectId = p.Id, OwnerId = p.Workspace.OwnerId })
                    .ToListAsync(cancellationToken);

                workspaceOwnersMap = wsOwners
                    .ToDictionary(x => x.ProjectId, x => x.OwnerId);
            }

            var targetRecipients = new List<(
                Guid CardId,
                string Title,
                string BoardSlug,
                Guid ProjectId,
                DateTimeOffset DueDate,
                bool IsOverdue,
                int DiffDays,
                bool IsUnassigned,
                Guid ReceiverId)>();
            // phân lạoi người nhận thông báo dựa trên Assignee, ProjectLeader hoặc WorkspaceOwner
            foreach (var card in CardsExpiredFrom7DayBeforeTo1DayLater)
            {
                if (card.AssigneeId.HasValue)
                {
                    targetRecipients.Add((
                        card.CardId,
                        card.Title,
                        card.BoardSlug,
                        card.ProjectId,
                        card.DueDate,
                        card.isOverdue,
                        card.diffDays,
                        false,
                        card.AssigneeId.Value));
                }
                else
                {
                    if (projectLeadersMap.TryGetValue(card.ProjectId, out var projectLeaderId))
                    {
                        targetRecipients.Add((
                             card.CardId,
                             card.Title,
                             card.BoardSlug,
                             card.ProjectId,
                             card.DueDate,
                             card.isOverdue,
                             card.diffDays,
                             true,
                             projectLeaderId));
                    }
                    else if (workspaceOwnersMap.TryGetValue(card.ProjectId, out var ownerId))
                    {
                        targetRecipients.Add((
                            card.CardId,
                            card.Title,
                            card.BoardSlug,
                            card.ProjectId,
                            card.DueDate,
                            card.isOverdue,
                            card.diffDays,
                            true,
                            ownerId));
                    }
                }
            }

            if (targetRecipients.Count == 0)
            {
                return;
            }

            var timeToCompare = now.AddHours(-20);// 20 tiếng trước tính từ time hiện tại
            var cardIdsToSend = targetRecipients.Select(r => r.CardId).Distinct().ToList();

            var recentNotiSended = await db.Notifications
                .AsNoTracking()
                .Where(n => n.Type == NotificationType.DueDateReminder
                            && n.EntityType == EntityType.Card
                            && n.EntityId.HasValue
                            && cardIdsToSend.Contains(n.EntityId.Value)
                            && n.CreatedAt >= timeToCompare)
                .Select(n => new { CardId = n.EntityId!.Value, n.ReceiverId })
                .ToListAsync(cancellationToken);

            var recentNotiSendedSet = new HashSet<(Guid CardId, Guid ReceiverId)>(
                recentNotiSended.Select(p => (p.CardId, p.ReceiverId)));

            var itemsToDispatch = targetRecipients
                .Where(tr => !recentNotiSendedSet.Contains((tr.CardId, tr.ReceiverId)))
                .Select(tr => NotificationMapper.BuildDueDateReminderItem(
                    receiverId: tr.ReceiverId,
                    cardId: tr.CardId,
                    cardTitle: tr.Title,
                    boardSlug: tr.BoardSlug,
                    projectId: tr.ProjectId,
                    isOverdue: tr.IsOverdue,
                    dueDate: tr.DueDate,
                    isUnassigned: tr.IsUnassigned,
                    diffDays: tr.DiffDays))
                .ToList();

            if (itemsToDispatch.Count == 0)
            {
                return;
            }

            // 5. Gửi thông báo (Lưu DB + SignalR push nếu đang online)
            await notificationService.CreateAndSendManyAsync(itemsToDispatch, cancellationToken);
            _logger.LogInformation("CardDueDateReminderWorker: Đã gửi {Count} thông báo nhắc hạn chót thẻ công việc.", itemsToDispatch.Count);
        }
    }
}
