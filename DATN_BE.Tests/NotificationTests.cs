using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Modules.Notifications.Application.Commands.MarkAllNotificationsAsRead;
using DATN_BE.Modules.Notifications.Application.Commands.MarkNotificationAsRead;
using DATN_BE.Modules.Notifications.Application.DTOs;
using DATN_BE.Modules.Notifications.Application.Queries.GetNotifications;
using DATN_BE.Modules.Notifications.Domain.Entities;
using DATN_BE.Modules.Notifications.Domain.Enum;
using DATN_BE.Modules.Notifications.Infrastructure.Services;
using DATN_BE.Modules.Notifications.Presentation.Hubs;
using DATN_BE.Modules.ProjectManagement.Application.Commands.AddMemberToProejct;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.Task.Application.Commands.CreateCard;
using DATN_BE.Modules.Task.Application.Commands.UpdateCard;
using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Modules.Task.Domain.Enum;
using DATN_BE.Modules.Workspaces.Application.Commands.AddWorkspaceMember;
using DATN_BE.Modules.Workspaces.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using DATN_BE.Shared.Enum;
using DATN_BE.Shared.Infrastructure.Persistence;
using DATN_BE.Shared.Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DATN_BE.Tests;

public class NotificationTests
{
    [Fact]
    public async Task Add_member_to_workspace_persists_and_pushes_workspace_invite_notification()
    {
        await using var f = await NotificationFixture.Create();
        f.Current.UserId = f.Owner.Id;

        var handler = new AddMemberToWorkspaceCommandHandler(f.Db, f.Current, f.NotificationService);
        var result = await handler.Handle(new AddMemberToWorkspaceCommand
        {
            WorkspaceId = f.Workspace.Id,
            Members =
            [
                new MemberEntry { Email = f.InviteTarget.Email },
                new MemberEntry { Email = f.ExistingWorkspaceMember.Email },
                new MemberEntry { Email = f.Owner.Email }
            ]
        }, CancellationToken.None);

        Assert.Equal(1, result.TotalSuccess);

        var notifications = await f.Db.Notifications
            .Where(n => n.ReceiverId == f.InviteTarget.Id)
            .ToListAsync();

        var persisted = Assert.Single(notifications);
        Assert.Equal(f.Owner.Id, persisted.SenderId);
        Assert.Equal(NotificationType.WorkspaceInvite, persisted.Type);
        Assert.Equal(EntityType.Workspace, persisted.EntityType);
        Assert.Equal(f.Workspace.Id, persisted.EntityId);
        Assert.False(persisted.IsRead);
        Assert.Contains(f.Workspace.Name, persisted.Content);

        // Không tự gửi thông báo cho chính Owner hoặc thành viên đã tồn tại
        Assert.DoesNotContain(await f.Db.Notifications.ToListAsync(), n => n.ReceiverId == f.Owner.Id);

        var pushed = Assert.Single(f.FakeHub.SentMessages);
        Assert.Equal(NotificationHub.UserGroupName(f.InviteTarget.Id), pushed.GroupName);
        Assert.Equal(NotificationHub.NotificationReceived, pushed.MethodName);
        var dto = Assert.IsType<NotificationDto>(pushed.Payload);
        Assert.Equal($"/workspaces/{f.Workspace.Slug}", dto.TargetUrl);
        Assert.Equal(f.Owner.Username, dto.SenderName);
    }

    [Fact]
    public async Task Add_member_to_project_persists_and_pushes_project_invite_notification()
    {
        await using var f = await NotificationFixture.Create();
        f.Current.UserId = f.Owner.Id;

        // Đảm bảo target thuộc workspace trước khi thêm vào dự án
        f.Db.WorkspaceMembers.Add(new WorkspaceMember
        {
            WorkspaceId = f.Workspace.Id,
            UserId = f.InviteTarget.Id,
            RoleId = f.WsMemberRole.Id
        });
        await f.Db.SaveChangesAsync();

        var handler = new AddMemberToProejctCommandHandler(f.Db, f.Current, f.NotificationService);
        var added = await handler.Handle(new AddMemberToProejctCommand
        {
            ProjectId = f.Project.Id,
            Members =
            [
                new AddProjectMemberEntry { UserId = f.InviteTarget.Id, JobTitle = "Backend Dev" }
            ]
        }, CancellationToken.None);

        Assert.Single(added);

        var persisted = await f.Db.Notifications
            .SingleAsync(n => n.ReceiverId == f.InviteTarget.Id);

        Assert.Equal(f.Owner.Id, persisted.SenderId);
        Assert.Equal(NotificationType.ProjectInvite, persisted.Type);
        Assert.Equal(EntityType.Project, persisted.EntityType);
        Assert.Equal(f.Project.Id, persisted.EntityId);
        Assert.Contains(f.Project.Name, persisted.Content);

        var pushed = Assert.Single(f.FakeHub.SentMessages);
        Assert.Equal(NotificationHub.UserGroupName(f.InviteTarget.Id), pushed.GroupName);
        Assert.Equal(NotificationHub.NotificationReceived, pushed.MethodName);
        var dto = Assert.IsType<NotificationDto>(pushed.Payload);
        Assert.Equal($"/workspaces/{f.Workspace.Id}/projects/{f.Project.ProjectKey}", dto.TargetUrl);
    }

    [Fact]
    public async Task Get_notifications_filters_by_current_user_and_resolves_target_urls()
    {
        await using var f = await NotificationFixture.Create();
        var now = DateTimeOffset.UtcNow;

        var wsNotif = new Notification
        {
            ReceiverId = f.InviteTarget.Id,
            SenderId = f.Owner.Id,
            Title = "WS",
            Content = "Workspace invite",
            Type = NotificationType.WorkspaceInvite,
            EntityType = EntityType.Workspace,
            EntityId = f.Workspace.Id,
            IsRead = false
        };
        f.Db.Notifications.Add(wsNotif);
        await f.Db.SaveChangesAsync();

        var projectNotif = new Notification
        {
            ReceiverId = f.InviteTarget.Id,
            SenderId = f.Owner.Id,
            Title = "Project",
            Content = "Project invite",
            Type = NotificationType.ProjectInvite,
            EntityType = EntityType.Project,
            EntityId = f.Project.Id,
            IsRead = true,
            ReadAt = now.AddMinutes(-1)
        };
        var otherNotif = new Notification
        {
            ReceiverId = f.OtherUser.Id,
            SenderId = f.Owner.Id,
            Title = "Other",
            Content = "Other user notification",
            Type = NotificationType.SystemAlert,
            IsRead = false
        };
        f.Db.Notifications.AddRange(projectNotif, otherNotif);
        await f.Db.SaveChangesAsync();

        // Đảm bảo thứ tự CreatedAt tách biệt rõ dù SaveChanges ghi CreatedAt = UtcNow khi Added
        wsNotif.CreatedAt = now.AddMinutes(-5);
        projectNotif.CreatedAt = now.AddMinutes(-1);
        await f.Db.SaveChangesAsync();

        f.Current.UserId = f.InviteTarget.Id;
        var queryHandler = new GetNotificationsQueryHandler(f.Db, f.Current);

        var allResult = await queryHandler.Handle(new GetNotificationsQuery(1, 20, false), CancellationToken.None);
        Assert.Equal(1, allResult.UnreadCount);
        Assert.Equal(2, allResult.TotalCount);
        Assert.False(allResult.HasMore);
        Assert.Equal(2, allResult.Items.Count);
        Assert.Equal($"/workspaces/{f.Workspace.Id}/projects/{f.Project.ProjectKey}", allResult.Items[0].TargetUrl);
        Assert.Equal($"/workspaces/{f.Workspace.Slug}", allResult.Items[1].TargetUrl);

        var unreadOnlyResult = await queryHandler.Handle(new GetNotificationsQuery(1, 20, true), CancellationToken.None);
        Assert.Equal(1, unreadOnlyResult.UnreadCount);
        Assert.Equal(1, unreadOnlyResult.TotalCount);
        Assert.Single(unreadOnlyResult.Items);
        Assert.False(unreadOnlyResult.Items[0].IsRead);
    }

    [Fact]
    public async Task Mark_as_read_and_mark_all_enforce_receiver_ownership()
    {
        await using var f = await NotificationFixture.Create();

        var notif1 = new Notification
        {
            ReceiverId = f.InviteTarget.Id,
            SenderId = f.Owner.Id,
            Title = "N1",
            Content = "First",
            Type = NotificationType.WorkspaceInvite,
            EntityType = EntityType.Workspace,
            EntityId = f.Workspace.Id,
            IsRead = false
        };
        var notif2 = new Notification
        {
            ReceiverId = f.InviteTarget.Id,
            SenderId = f.Owner.Id,
            Title = "N2",
            Content = "Second",
            Type = NotificationType.ProjectInvite,
            EntityType = EntityType.Project,
            EntityId = f.Project.Id,
            IsRead = false
        };
        var otherNotif = new Notification
        {
            ReceiverId = f.OtherUser.Id,
            SenderId = f.Owner.Id,
            Title = "Other",
            Content = "Third",
            Type = NotificationType.SystemAlert,
            IsRead = false
        };
        f.Db.Notifications.AddRange(notif1, notif2, otherNotif);
        await f.Db.SaveChangesAsync();

        // Người dùng khác không thể đánh dấu thông báo của người nhận
        f.Current.UserId = f.OtherUser.Id;
        var markOneHandler = new MarkNotificationAsReadCommandHandler(f.Db, f.Current, f.FakeHub);
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            markOneHandler.Handle(new MarkNotificationAsReadCommand(notif1.Id), CancellationToken.None));

        // Đánh dấu 1 thông báo của chính mình
        f.Current.UserId = f.InviteTarget.Id;
        var markedDto = await markOneHandler.Handle(new MarkNotificationAsReadCommand(notif1.Id), CancellationToken.None);
        Assert.True(markedDto.IsRead);
        Assert.NotNull(markedDto.ReadAt);

        // Đánh dấu tất cả phần còn lại cho InviteTarget
        var markAllHandler = new MarkAllNotificationsAsReadCommandHandler(f.Db, f.Current, f.FakeHub);
        var updatedCount = await markAllHandler.Handle(new MarkAllNotificationsAsReadCommand(), CancellationToken.None);
        Assert.Equal(1, updatedCount);
        Assert.True((await f.Db.Notifications.SingleAsync(n => n.Id == notif2.Id)).IsRead);
        Assert.False((await f.Db.Notifications.SingleAsync(n => n.Id == otherNotif.Id)).IsRead);
    }

    [Fact]
    public async Task Notification_service_persists_to_database_even_when_signalr_push_fails()
    {
        await using var f = await NotificationFixture.Create(throwOnHubSend: true);

        await f.NotificationService.CreateAndSendManyAsync(
            [
                new NotificationDispatchItem(
                    ReceiverId: f.InviteTarget.Id,
                    SenderId: f.Owner.Id,
                    SenderName: f.Owner.Username,
                    SenderAvatarUrl: null,
                    Type: NotificationType.WorkspaceInvite,
                    Title: "Test",
                    Content: "Must still persist",
                    EntityType: EntityType.Workspace,
                    EntityId: f.Workspace.Id,
                    TargetUrl: $"/workspaces/{f.Workspace.Slug}")
            ],
            CancellationToken.None);

        var saved = await f.Db.Notifications.SingleAsync(n => n.ReceiverId == f.InviteTarget.Id);
        Assert.Equal("Must still persist", saved.Content);
    }

    [Fact]
    public async Task Create_card_with_assignee_persists_and_pushes_card_assigned_notification()
    {
        await using var f = await NotificationFixture.Create();
        await f.SeedBoardWithMembersAsync();
        f.Current.UserId = f.Owner.Id;

        var handler = new CreateCardCommandHandler(
            f.Db,
            f.Current,
            f.Auth,
            notificationService: f.NotificationService);

        var created = await handler.Handle(new CreateCardCommand
        {
            ColumnId = f.Column.Id,
            Title = "Implement Login API",
            AssigneeId = f.InviteTarget.Id
        }, CancellationToken.None);

        var persisted = await f.Db.Notifications.SingleAsync(n => n.ReceiverId == f.InviteTarget.Id);
        Assert.Equal(f.Owner.Id, persisted.SenderId);
        Assert.Equal(NotificationType.CardAssigned, persisted.Type);
        Assert.Equal(EntityType.Card, persisted.EntityType);
        Assert.Equal(created.Id, persisted.EntityId);
        Assert.Contains("Implement Login API", persisted.Content);

        var pushed = Assert.Single(f.FakeHub.SentMessages);
        Assert.Equal(NotificationHub.UserGroupName(f.InviteTarget.Id), pushed.GroupName);
        var dto = Assert.IsType<NotificationDto>(pushed.Payload);
        Assert.Equal($"/boards/{f.Board.Slug}?projectId={f.Project.Id}&cardId={created.Id}", dto.TargetUrl);

        // Kiểm tra GetNotificationsQuery resolve đúng TargetUrl cho EntityType.Card
        f.Current.UserId = f.InviteTarget.Id;
        var feed = await new GetNotificationsQueryHandler(f.Db, f.Current)
            .Handle(new GetNotificationsQuery(), CancellationToken.None);
        var fetched = Assert.Single(feed.Items);
        Assert.Equal($"/boards/{f.Board.Slug}?projectId={f.Project.Id}&cardId={created.Id}", fetched.TargetUrl);
    }

    [Fact]
    public async Task Update_card_assignee_notifies_only_when_assignee_changes_to_another_user()
    {
        await using var f = await NotificationFixture.Create();
        await f.SeedBoardWithMembersAsync();

        var card = new Card
        {
            ColumnId = f.Column.Id,
            Title = "Build Kanban UI",
            ReporterId = f.Owner.Id,
            AssigneeId = null
        };
        f.Db.Cards.Add(card);
        await f.Db.SaveChangesAsync();

        f.Current.UserId = f.Owner.Id;
        var handler = new UpdateCardCommandHandler(
            f.Db,
            f.Auth,
            currentUserService: f.Current,
            notificationService: f.NotificationService);

        // 1. Gán mới từ null -> InviteTarget => phải gửi 1 thông báo
        await handler.Handle(new UpdateCardCommand
        {
            CardId = card.Id,
            Title = "Build Kanban UI",
            Type = CardType.Task,
            Priority = CardPriority.Medium,
            AssigneeId = f.InviteTarget.Id
        }, CancellationToken.None);

        Assert.Equal(1, await f.Db.Notifications.CountAsync(n => n.ReceiverId == f.InviteTarget.Id));
        Assert.Single(f.FakeHub.SentMessages);

        // 2. Cập nhật tiêu đề nhưng giữ nguyên AssigneeId => không spam thông báo lặp
        await handler.Handle(new UpdateCardCommand
        {
            CardId = card.Id,
            Title = "Build Kanban UI v2",
            Type = CardType.Task,
            Priority = CardPriority.High,
            AssigneeId = f.InviteTarget.Id
        }, CancellationToken.None);

        Assert.Equal(1, await f.Db.Notifications.CountAsync(n => n.ReceiverId == f.InviteTarget.Id));
        Assert.Single(f.FakeHub.SentMessages);

        // 3. Tự gán cho chính mình (Owner) => không tự gửi thông báo
        await handler.Handle(new UpdateCardCommand
        {
            CardId = card.Id,
            Title = "Build Kanban UI v2",
            Type = CardType.Task,
            Priority = CardPriority.High,
            AssigneeId = f.Owner.Id
        }, CancellationToken.None);

        Assert.Equal(0, await f.Db.Notifications.CountAsync(n => n.ReceiverId == f.Owner.Id));
        Assert.Single(f.FakeHub.SentMessages);
    }

    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId { get; set; }
        public string? Email => "owner@example.com";
        public string? Username => "owner";
        public bool IsAuthenticated => UserId.HasValue;
    }

    private sealed record HubPushMessage(string GroupName, string MethodName, object? Payload);

    private sealed class FakeNotificationHubContext(bool throwOnSend) : IHubContext<NotificationHub>
    {
        public List<HubPushMessage> SentMessages { get; } = new();
        public IHubClients Clients => new FakeHubClients(SentMessages, throwOnSend);
        public IGroupManager Groups => throw new NotImplementedException();

        private sealed class FakeHubClients(List<HubPushMessage> sent, bool throwOnSend) : IHubClients
        {
            public IClientProxy All => throw new NotImplementedException();
            public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
            public IClientProxy Client(string connectionId) => throw new NotImplementedException();
            public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotImplementedException();
            public IClientProxy Group(string groupName) => new FakeGroupProxy(groupName, sent, throwOnSend);
            public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
            public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotImplementedException();
            public IClientProxy User(string userId) => throw new NotImplementedException();
            public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotImplementedException();
        }

        private sealed class FakeGroupProxy(string groupName, List<HubPushMessage> sent, bool throwOnSend) : IClientProxy
        {
            public System.Threading.Tasks.Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
            {
                if (throwOnSend)
                {
                    throw new InvalidOperationException("Simulated SignalR transport failure");
                }

                sent.Add(new HubPushMessage(groupName, method, args.FirstOrDefault()));
                return System.Threading.Tasks.Task.CompletedTask;
            }
        }
    }

    private sealed class NotificationFixture : IAsyncDisposable
    {
        public CurrentUser Current { get; } = new();
        public ApplicationDbContext Db { get; private set; } = null!;
        public FakeNotificationHubContext FakeHub { get; private set; } = null!;
        public NotificationService NotificationService { get; private set; } = null!;

        public User Owner { get; } = new() { Username = "owner", Email = "owner@example.com", IsActive = true };
        public User ExistingWorkspaceMember { get; } = new() { Username = "existing", Email = "existing@example.com", IsActive = true };
        public User InviteTarget { get; } = new() { Username = "target", Email = "target@example.com", IsActive = true };
        public User OtherUser { get; } = new() { Username = "other", Email = "other@example.com", IsActive = true };

        public Role SystemUserRole { get; } = new() { Name = RoleConstants.User, Scope = RoleScope.Global };
        public Role WsMemberRole { get; } = new() { Name = RoleConstants.WorkspaceMember, Scope = RoleScope.Workspace };
        public Role ProjectMemberRole { get; } = new() { Name = RoleConstants.ProjectMember, Scope = RoleScope.Project };

        public Workspace Workspace { get; private set; } = null!;
        public Project Project { get; private set; } = null!;
        public Board Board { get; private set; } = null!;
        public Column Column { get; private set; } = null!;
        public ProjectAuthorizationService Auth => new(Db, Current);

        public async System.Threading.Tasks.Task SeedBoardWithMembersAsync()
        {
            Db.ProjectMembers.AddRange(
                new ProjectMember
                {
                    ProjectId = Project.Id,
                    UserId = Owner.Id,
                    RoleId = ProjectMemberRole.Id
                },
                new ProjectMember
                {
                    ProjectId = Project.Id,
                    UserId = InviteTarget.Id,
                    RoleId = ProjectMemberRole.Id
                });

            Board = new Board
            {
                Title = "Sprint Board",
                Slug = "sprint-board",
                ProjectId = Project.Id
            };
            Db.Boards.Add(Board);

            Column = new Column
            {
                Title = "To Do",
                Position = 0,
                BoardId = Board.Id
            };
            Db.Columns.Add(Column);

            await Db.SaveChangesAsync();
        }

        public static async Task<NotificationFixture> Create(bool throwOnHubSend = false)
        {
            var f = new NotificationFixture();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            f.Db = new ApplicationDbContext(options, f.Current);
            f.Db.Roles.AddRange(f.SystemUserRole, f.WsMemberRole, f.ProjectMemberRole);

            foreach (var user in new[] { f.Owner, f.ExistingWorkspaceMember, f.InviteTarget, f.OtherUser })
            {
                user.RoleId = f.SystemUserRole.Id;
                f.Db.Users.Add(user);
            }

            f.Workspace = new Workspace
            {
                Name = "Team Alpha",
                Slug = "team-alpha",
                OwnerId = f.Owner.Id
            };
            f.Db.Workspaces.Add(f.Workspace);

            f.Db.WorkspaceMembers.AddRange(
                new WorkspaceMember
                {
                    WorkspaceId = f.Workspace.Id,
                    UserId = f.Owner.Id,
                    RoleId = f.WsMemberRole.Id
                },
                new WorkspaceMember
                {
                    WorkspaceId = f.Workspace.Id,
                    UserId = f.ExistingWorkspaceMember.Id,
                    RoleId = f.WsMemberRole.Id
                });

            f.Project = new Project
            {
                Name = "Core Portal",
                ProjectKey = "CORE",
                WorkspaceId = f.Workspace.Id,
                CreatedById = f.Owner.Id
            };
            f.Db.Projects.Add(f.Project);

            await f.Db.SaveChangesAsync();
            f.FakeHub = new FakeNotificationHubContext(throwOnHubSend);
            f.NotificationService = new NotificationService(f.Db, f.FakeHub);
            return f;
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}
