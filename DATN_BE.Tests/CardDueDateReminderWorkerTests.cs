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
    public async Task Worker_SendsDueDateReminders_ForUpcomingAndOverdueCards_WithoutDuplicates_AndHandlesUnassignedCards()
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

        Guid assigneeUserId;
        Guid projectLeaderUserId;
        Guid regularMemberUserId;
        Guid wsOwnerUserId;

        // Seed data
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var globalRole = new Role { Name = RoleConstants.User, Scope = RoleScope.Global };
            var leaderRole = new Role { Name = RoleConstants.ProjectLeader, Scope = RoleScope.Project };
            var memberRole = new Role { Name = RoleConstants.ProjectMember, Scope = RoleScope.Project };
            db.Roles.AddRange(globalRole, leaderRole, memberRole);

            var assigneeUser = new User { Username = "worker_target", Email = "target@test.com", RoleId = globalRole.Id };
            var leaderUser = new User { Username = "project_leader", Email = "leader@test.com", RoleId = globalRole.Id };
            var regularUser = new User { Username = "regular_member", Email = "member@test.com", RoleId = globalRole.Id };
            var wsOwnerUser = new User { Username = "ws_owner_only", Email = "owner@test.com", RoleId = globalRole.Id };
            db.Users.AddRange(assigneeUser, leaderUser, regularUser, wsOwnerUser);

            assigneeUserId = assigneeUser.Id;
            projectLeaderUserId = leaderUser.Id;
            regularMemberUserId = regularUser.Id;
            wsOwnerUserId = wsOwnerUser.Id;

            // Workspace 1 & Project 1 (có Leader và Member)
            var ws1 = new Workspace { Name = "WS1", Slug = "ws1", OwnerId = assigneeUser.Id };
            db.Workspaces.Add(ws1);

            var prj1 = new Project { Name = "Prj1", ProjectKey = "PRJ1", WorkspaceId = ws1.Id, CreatedById = assigneeUser.Id };
            db.Projects.Add(prj1);

            var pmLeader = new ProjectMember { ProjectId = prj1.Id, UserId = leaderUser.Id, RoleId = leaderRole.Id };
            var pmRegular = new ProjectMember { ProjectId = prj1.Id, UserId = regularUser.Id, RoleId = memberRole.Id };
            db.ProjectMembers.AddRange(pmLeader, pmRegular);

            var board1 = new Board { Title = "Board 1", Slug = "board-slug-1", ProjectId = prj1.Id };
            db.Boards.Add(board1);

            var todoCol1 = new Column { Title = "To Do", BoardId = board1.Id, Position = 0, IsDoneColumn = false };
            var doneCol1 = new Column { Title = "Done", BoardId = board1.Id, Position = 1, IsDoneColumn = true };
            db.Columns.AddRange(todoCol1, doneCol1);

            // Workspace 2 & Project 2 (không có Leader -> fallback về WorkspaceOwner)
            var ws2 = new Workspace { Name = "WS2", Slug = "ws2", OwnerId = wsOwnerUser.Id };
            db.Workspaces.Add(ws2);

            var prj2 = new Project { Name = "Prj2", ProjectKey = "PRJ2", WorkspaceId = ws2.Id, CreatedById = wsOwnerUser.Id };
            db.Projects.Add(prj2);

            var board2 = new Board { Title = "Board 2", Slug = "board-slug-2", ProjectId = prj2.Id };
            db.Boards.Add(board2);

            var todoCol2 = new Column { Title = "To Do 2", BoardId = board2.Id, Position = 0, IsDoneColumn = false };
            db.Columns.Add(todoCol2);

            var now = DateTimeOffset.UtcNow;

            // Thẻ 1: Sắp đến hạn trong 12 tiếng tới (có Assignee) -> CẦN gửi cho Assignee
            var cardDueSoon = new Card
            {
                Title = "Task Due Soon",
                ColumnId = todoCol1.Id,
                AssigneeId = assigneeUser.Id,
                ReporterId = assigneeUser.Id,
                DueDate = now.AddHours(12),
                IsCompleted = false
            };

            // Thẻ 2: Đã quá hạn 1 ngày (có Assignee) -> CẦN gửi cho Assignee
            var cardOverdue = new Card
            {
                Title = "Task Overdue",
                ColumnId = todoCol1.Id,
                AssigneeId = assigneeUser.Id,
                ReporterId = assigneeUser.Id,
                DueDate = now.AddDays(-1),
                IsCompleted = false
            };

            // Thẻ 3: Đã hoàn thành (IsCompleted = true) -> KHÔNG gửi
            var cardCompleted = new Card
            {
                Title = "Task Completed",
                ColumnId = todoCol1.Id,
                AssigneeId = assigneeUser.Id,
                ReporterId = assigneeUser.Id,
                DueDate = now.AddHours(6),
                IsCompleted = true
            };

            // Thẻ 4: Nằm trong cột Done -> KHÔNG gửi
            var cardInDoneCol = new Card
            {
                Title = "Task In Done Column",
                ColumnId = doneCol1.Id,
                AssigneeId = assigneeUser.Id,
                ReporterId = assigneeUser.Id,
                DueDate = now.AddHours(6),
                IsCompleted = false
            };

            // Thẻ 5: Không có Assignee, quá hạn trong Prj 1 (có ProjectLeader) -> CẦN gửi cho ProjectLeader (KHÔNG gửi regular member)
            var cardUnassignedLeader = new Card
            {
                Title = "Task Unassigned Overdue With Leader",
                ColumnId = todoCol1.Id,
                AssigneeId = null,
                ReporterId = assigneeUser.Id,
                DueDate = now.AddDays(-2),
                IsCompleted = false
            };

            // Thẻ 6: Không có Assignee, quá hạn trong Prj 2 (không có Leader) -> CẦN gửi cho WorkspaceOwner (wsOwnerUser)
            var cardUnassignedOwner = new Card
            {
                Title = "Task Unassigned Overdue Fallback Owner",
                ColumnId = todoCol2.Id,
                AssigneeId = null,
                ReporterId = wsOwnerUser.Id,
                DueDate = now.AddDays(-1),
                IsCompleted = false
            };

            db.Cards.AddRange(cardDueSoon, cardOverdue, cardCompleted, cardInDoneCol, cardUnassignedLeader, cardUnassignedOwner);
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

            // Tổng cộng 4 thông báo được gửi:
            // 1) cardDueSoon -> assigneeUserId
            // 2) cardOverdue -> assigneeUserId
            // 3) cardUnassignedLeader -> projectLeaderUserId
            // 4) cardUnassignedOwner -> wsOwnerUserId
            Assert.Equal(4, notifications.Count);

            Assert.Contains(notifications, n => n.Title.Contains("quá hạn"));
            Assert.Contains(notifications, n => n.Title.Contains("sắp đến hạn"));

            // Kiểm tra phân phối người nhận chính xác:
            var assigneeNotifs = notifications.Where(n => n.ReceiverId == assigneeUserId).ToList();
            Assert.Equal(2, assigneeNotifs.Count);

            var leaderNotifs = notifications.Where(n => n.ReceiverId == projectLeaderUserId).ToList();
            Assert.Single(leaderNotifs);
            Assert.Contains("chưa phân công", leaderNotifs[0].Title);

            var ownerNotifs = notifications.Where(n => n.ReceiverId == wsOwnerUserId).ToList();
            Assert.Single(ownerNotifs);
            Assert.Contains("chưa phân công", ownerNotifs[0].Title);

            // Thành viên thông thường (regular member) tuyệt đối không nhận thông báo spam
            var regularMemberNotifs = notifications.Where(n => n.ReceiverId == regularMemberUserId).ToList();
            Assert.Empty(regularMemberNotifs);
        }

        // Lần 2: Chạy quét lại ngay sau đó -> Không được gửi trùng (Chống spam)
        await worker.CheckAndSendRemindersAsync(CancellationToken.None);

        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notifications = await db.Notifications
                .Where(n => n.Type == NotificationType.DueDateReminder)
                .ToListAsync();

            // Số lượng vẫn là 4, không tăng thêm
            Assert.Equal(4, notifications.Count);
        }
    }
}
