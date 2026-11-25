using System.Text.Json;
using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Modules.ProjectManagement.Application.Queries.GetColumnActivities;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.Security.Application.EventHandlers;
using DATN_BE.Modules.Security.Domain.Entities;
using DATN_BE.Modules.Security.Domain.Enum;
using DATN_BE.Modules.Task.Application.Queries.GetCardActivities;
using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Modules.Workspaces.Domain.Entities;
using DATN_BE.Shared.Application.Events;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using DATN_BE.Shared.Infrastructure.Persistence;
using DATN_BE.Shared.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DATN_BE.Tests;

public class CardColumnActivityTests
{
    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }
        public bool IsAuthenticated => UserId.HasValue;
    }

    private static async Task<(ApplicationDbContext Db, CurrentUser Current, ProjectAuthorizationService Auth, User Member, User Outsider, Card Card, Column Column)> SetupAsync()
    {
        var current = new CurrentUser();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new ApplicationDbContext(options, current);

        var memberRole = new Role { Name = RoleConstants.ProjectMember, Scope = RoleScope.Project };
        var leaderRole = new Role { Name = RoleConstants.ProjectLeader, Scope = RoleScope.Project };
        db.Roles.AddRange(memberRole, leaderRole);

        var owner = new User { Username = "owner", Email = "owner@test.com", RoleId = memberRole.Id };
        var member = new User { Username = "member", Email = "member@test.com", RoleId = memberRole.Id };
        var outsider = new User { Username = "outsider", Email = "outsider@test.com", RoleId = memberRole.Id };
        db.Users.AddRange(owner, member, outsider);

        var workspace = new Workspace { Name = "Workspace", Slug = "ws", OwnerId = owner.Id };
        db.Workspaces.Add(workspace);

        var project = new Project { Name = "Project", ProjectKey = "PRJ", WorkspaceId = workspace.Id, CreatedById = owner.Id };
        db.Projects.Add(project);

        db.WorkspaceMembers.Add(new WorkspaceMember { WorkspaceId = workspace.Id, UserId = member.Id, RoleId = memberRole.Id });
        db.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = member.Id, RoleId = memberRole.Id });

        var board = new Board { ProjectId = project.Id, Title = "Board", Slug = "board" };
        db.Boards.Add(board);

        var column = new Column { BoardId = board.Id, Title = "To Do" };
        db.Columns.Add(column);

        var card = new Card { ColumnId = column.Id, Title = "Task 1", ReporterId = member.Id };
        db.Cards.Add(card);

        await db.SaveChangesAsync();

        var auth = new ProjectAuthorizationService(db, current);
        return (db, current, auth, member, outsider, card, column);
    }

    [Fact]
    public async Task EntityActivityLoggedEventHandler_saves_activity_log()
    {
        var (db, current, auth, member, outsider, card, column) = await SetupAsync();
        var handler = new EntityActivityLoggedEventHandler(db);

        var cardEvent = new EntityActivityLoggedEvent(
            member.Id,
            ActivityEntityType.Card,
            card.Id,
            ActivityActionType.MovedToColumn,
            new { fromColumn = "To Do", toColumn = "Done" });

        await handler.Handle(cardEvent, default);

        var savedLog = await db.ActivityLogs.FirstOrDefaultAsync(l => l.EntityId == card.Id);
        Assert.NotNull(savedLog);
        Assert.Equal(ActivityEntityType.Card, savedLog.EntityType);
        Assert.Equal(ActivityActionType.MovedToColumn, savedLog.ActionType);
        Assert.Contains("To Do", savedLog.DetailsJson!);
        Assert.Contains("Done", savedLog.DetailsJson!);
    }

    [Fact]
    public async Task GetCardActivitiesQueryHandler_returns_paged_logs_for_member()
    {
        var (db, current, auth, member, outsider, card, column) = await SetupAsync();
        current.UserId = member.Id;

        db.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = member.Id,
            EntityType = ActivityEntityType.Card,
            EntityId = card.Id,
            ActionType = ActivityActionType.Created,
            CreatedAt = DateTimeOffset.UtcNow,
            DetailsJson = "{\"cardTitle\":\"Task 1\"}"
        });
        await db.SaveChangesAsync();

        var handler = new GetCardActivitiesQueryHandler(db, auth);
        var result = await handler.Handle(new GetCardActivitiesQuery(card.Id, 1, 10), default);

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Created", result.Items[0].ActionType);
        Assert.Equal(member.Username, result.Items[0].Username);
    }

    [Fact]
    public async Task GetCardActivitiesQueryHandler_rejects_outsider()
    {
        var (db, current, auth, member, outsider, card, column) = await SetupAsync();
        current.UserId = outsider.Id;

        var handler = new GetCardActivitiesQueryHandler(db, auth);
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new GetCardActivitiesQuery(card.Id, 1, 10), default));
    }

    [Fact]
    public async Task GetColumnActivitiesQueryHandler_returns_paged_logs_for_member()
    {
        var (db, current, auth, member, outsider, card, column) = await SetupAsync();
        current.UserId = member.Id;

        db.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = member.Id,
            EntityType = ActivityEntityType.Column,
            EntityId = column.Id,
            ActionType = ActivityActionType.Created,
            CreatedAt = DateTimeOffset.UtcNow,
            DetailsJson = "{\"columnTitle\":\"To Do\"}"
        });
        await db.SaveChangesAsync();

        var handler = new GetColumnActivitiesQueryHandler(db, auth);
        var result = await handler.Handle(new GetColumnActivitiesQuery(column.Id, 1, 10), default);

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Created", result.Items[0].ActionType);
        Assert.Equal(member.Username, result.Items[0].Username);
    }

    [Fact]
    public async Task GetColumnActivitiesQueryHandler_rejects_outsider()
    {
        var (db, current, auth, member, outsider, card, column) = await SetupAsync();
        current.UserId = outsider.Id;

        var handler = new GetColumnActivitiesQueryHandler(db, auth);
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new GetColumnActivitiesQuery(column.Id, 1, 10), default));
    }

    [Fact]
    public async Task EntityActivityLoggedEventHandler_saves_card_type_and_storypoints_updates()
    {
        var (db, current, auth, member, outsider, card, column) = await SetupAsync();
        var handler = new EntityActivityLoggedEventHandler(db);

        var typeEvent = new EntityActivityLoggedEvent(
            member.Id,
            ActivityEntityType.Card,
            card.Id,
            ActivityActionType.Updated,
            new { cardTitle = card.Title, field = "Type", oldType = "Task", newType = "Bug" });

        var storyPointsEvent = new EntityActivityLoggedEvent(
            member.Id,
            ActivityEntityType.Card,
            card.Id,
            ActivityActionType.Updated,
            new { cardTitle = card.Title, field = "StoryPoints", oldStoryPoints = (int?)null, newStoryPoints = 5 });

        await handler.Handle(typeEvent, default);
        await handler.Handle(storyPointsEvent, default);

        var logs = await db.ActivityLogs.Where(l => l.EntityId == card.Id).ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.DetailsJson!.Contains("Bug"));
        Assert.Contains(logs, l => l.DetailsJson!.Contains("StoryPoints"));
    }
}
