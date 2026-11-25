using DATN_BE.Modules.Notifications.Application.Helpers;
using DATN_BE.Modules.Notifications.Domain.Enum;
using DATN_BE.Shared.Application.Interfaces;
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
    /// tự động gửi thông báo (DueDateReminder) tới người thực hiện (Assignee).
    /// </summary>
    public class CardDueDateReminderWorker : BackgroundService
    {
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
            var soonThreshold = now.AddDays(1); // Thẻ sắp đến hạn trong vòng 24 giờ tới
            var overdueThreshold = now.AddDays(-7); // Thẻ quá hạn trong vòng 7 ngày gần đây

            // 1. Lấy danh sách thẻ thỏa mãn điều kiện
            var candidateCards = await db.Cards
                .AsNoTracking()
                .Include(c => c.Column).ThenInclude(col => col.Board)
                .Where(c => !c.IsCompleted
                            && c.AssigneeId.HasValue
                            && c.DueDate.HasValue
                            && c.DueDate.Value >= overdueThreshold
                            && c.DueDate.Value <= soonThreshold
                            && c.Column != null
                            && !c.Column.IsDoneColumn
                            && !c.Column.IsDeleted
                            && !c.Column.Board.IsDeleted)
                .Select(c => new
                {
                    CardId = c.Id,
                    c.Title,
                    AssigneeId = c.AssigneeId!.Value,
                    DueDate = c.DueDate!.Value,
                    BoardSlug = c.Column.Board.Slug,
                    ProjectId = c.Column.Board.ProjectId
                })
                .ToListAsync(cancellationToken);

            if (candidateCards.Count == 0)
            {
                return;
            }

            // 2. Chống spam thông báo: Kiểm tra xem thẻ nào đã được gửi thông báo DueDateReminder trong 20 giờ qua
            var recentNotificationCutoff = now.AddHours(-20);
            var cardIds = candidateCards.Select(c => c.CardId).ToList();

            var recentlyNotifiedCardIds = await db.Notifications
                .AsNoTracking()
                .Where(n => n.Type == NotificationType.DueDateReminder
                            && n.EntityType == EntityType.Card
                            && n.EntityId.HasValue
                            && cardIds.Contains(n.EntityId.Value)
                            && n.CreatedAt >= recentNotificationCutoff)
                .Select(n => n.EntityId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            var cardsToNotify = candidateCards
                .Where(c => !recentlyNotifiedCardIds.Contains(c.CardId))
                .ToList();

            if (cardsToNotify.Count == 0)
            {
                return;
            }

            // 3. Tạo dispatch items và gửi qua NotificationService (Pha 1 DB + Pha 2 SignalR)
            var dispatchItems = cardsToNotify.Select(c =>
            {
                var isOverdue = c.DueDate.Date < now.Date;
                return NotificationMapper.BuildDueDateReminderItem(
                    receiverId: c.AssigneeId,
                    cardId: c.CardId,
                    cardTitle: c.Title,
                    boardSlug: c.BoardSlug,
                    projectId: c.ProjectId,
                    isOverdue: isOverdue,
                    dueDate: c.DueDate);
            }).ToList();

            await notificationService.CreateAndSendManyAsync(dispatchItems, cancellationToken);
            _logger.LogInformation("CardDueDateReminderWorker: Đã gửi {Count} thông báo nhắc hạn chót thẻ công việc.", dispatchItems.Count);
        }
    }
}
