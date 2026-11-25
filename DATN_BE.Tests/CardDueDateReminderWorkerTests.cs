using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Modules.Notifications.Domain.Enum;
using DATN_BE.Modules.Notifications.Infrastructure.Services;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Modules.Task.Presentation.BackgroundJob;
using DATN_BE.Modules.Workspaces.Domain.Entities;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using DATN_BE.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DATN_BE.Tests;

public class CardDueDateReminderWorkerTests
{
    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }
        public bool IsAuthenticated => UserId.HasValue;
    }

    [Fact]
    public async Task Worker_SendsDueDateReminders_ForUpcomingAndOverdueCards_WithoutDuplicates()
    {
        var services = new ServiceCollection();
        var current = new CurrentUser();
        var dbName = Guid.NewGuid().ToString();

        services.AddDbContext<ApplicationDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddSingleton<ICurrentUserService>(current);
        services.AddScoped<INotificationService, NotificationService>();

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        // Seed data
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var role = new Role { Name = RoleConstants.User, Scope = RoleScope.Global };
            db.Roles.Add(role);

            var user = new User { Username = "worker_target", Email = "target@test.com", RoleId = role.Id };
            db.Users.Add(user);

            var ws = new Workspace { Name = "WS", Slug = "ws", OwnerId = user.Id };
            db.Workspaces.Add(ws);

            var prj = new Project { Name = "Prj", ProjectKey = "PRJ", WorkspaceId = ws.Id, CreatedById = user.Id };
            db.Projects.Add(prj);

            var board = new Board { Title = "Board", Slug = "board-slug", ProjectId = prj.Id };
            db.Boards.Add(board);

            var todoCol = new Column { Title = "To Do", BoardId = board.Id, Position = 0, IsDoneColumn = false };
            var doneCol = new Column { Title = "Done", BoardId = board.Id, Position = 1, IsDoneColumn = true };
            db.Columns.AddRange(todoCol, doneCol);

            var now = DateTimeOffset.UtcNow;

            // Thẻ 1: Sắp đến hạn trong 12 tiếng tới -> CẦN gửi nhắc nhở
            var cardDueSoon = new Card
            {
                Title = "Task Due Soon",
                ColumnId = todoCol.Id,
                AssigneeId = user.Id,
                ReporterId = user.Id,
                DueDate = now.AddHours(12),
                IsCompleted = false
            };

            // Thẻ 2: Đã quá hạn 1 ngày -> CẦN gửi thông báo quá hạn
            var cardOverdue = new Card
            {
                Title = "Task Overdue",
                ColumnId = todoCol.Id,
                AssigneeId = user.Id,
                ReporterId = user.Id,
                DueDate = now.AddDays(-1),
                IsCompleted = false
            };

            // Thẻ 3: Đã hoàn thành (IsCompleted = true) -> KHÔNG gửi
            var cardCompleted = new Card
            {
                Title = "Task Completed",
                ColumnId = todoCol.Id,
                AssigneeId = user.Id,
                ReporterId = user.Id,
                DueDate = now.AddHours(6),
                IsCompleted = true
            };

            // Thẻ 4: Nằm trong cột Done -> KHÔNG gửi
            var cardInDoneCol = new Card
            {
                Title = "Task In Done Column",
                ColumnId = doneCol.Id,
                AssigneeId = user.Id,
                ReporterId = user.Id,
                DueDate = now.AddHours(6),
                IsCompleted = false
            };

            // Thẻ 5: Không có Assignee -> KHÔNG gửi
            var cardNoAssignee = new Card
            {
                Title = "Task No Assignee",
                ColumnId = todoCol.Id,
                AssigneeId = null,
                ReporterId = user.Id,
                DueDate = now.AddHours(6),
                IsCompleted = false
            };

            db.Cards.AddRange(cardDueSoon, cardOverdue, cardCompleted, cardInDoneCol, cardNoAssignee);
            await db.SaveChangesAsync();
        }

        var worker = new CardDueDateReminderWorker(scopeFactory, NullLogger<CardDueDateReminderWorker>.Instance);

        // Lần 1: Chạy quét hạn chót
        await worker.CheckAndSendRemindersAsync(CancellationToken.None);

        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notifications = await db.Notifications
                .Where(n => n.Type == NotificationType.DueDateReminder)
                .ToListAsync();

            // Phải có đúng 2 thông báo: cho cardDueSoon và cardOverdue
            Assert.Equal(2, notifications.Count);
            Assert.Contains(notifications, n => n.Title.Contains("quá hạn"));
            Assert.Contains(notifications, n => n.Title.Contains("sắp đến hạn"));
        }

        // Lần 2: Chạy quét lại ngay sau đó -> Không được gửi trùng (Chống spam)
        await worker.CheckAndSendRemindersAsync(CancellationToken.None);

        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notifications = await db.Notifications
                .Where(n => n.Type == NotificationType.DueDateReminder)
                .ToListAsync();

            // Số lượng vẫn là 2, không tăng thêm
            Assert.Equal(2, notifications.Count);
        }
    }
}
