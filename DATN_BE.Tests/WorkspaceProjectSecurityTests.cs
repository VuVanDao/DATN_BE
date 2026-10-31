using System.Text.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Modules.ProjectManagement.Application.Commands.CreateBoard;
using DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteBoard;
using DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateBoard;
using DATN_BE.Modules.ProjectManagement.Application.Commands.CreateColumn;
using DATN_BE.Modules.ProjectManagement.Application.Commands.ReorderColumns;
using DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateProject;
using DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateProjectLeader;
using DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateProjectMemberRole;
using DATN_BE.Modules.ProjectManagement.Application.Commands.RemoveMemberFromProject;
using DATN_BE.Modules.ProjectManagement.Application.Commands.CreateProject;
using DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteProject;
using DATN_BE.Modules.ProjectManagement.Application.Queries.GetBoardDetail;
using DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectBoards;
using DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectAuditLogs;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.ProjectManagement.Presentation.Controllers;
using DATN_BE.Modules.Security.Domain.Entities;
using DATN_BE.Modules.Security.Domain.Enum;
using DATN_BE.Modules.Task.Application.Commands.AddCardComment;
using DATN_BE.Modules.Task.Application.Commands.CreateCard;
using DATN_BE.Modules.Task.Application.Commands.DeleteCard;
using DATN_BE.Modules.Task.Application.Commands.MoveCard;
using DATN_BE.Modules.Task.Application.Commands.UpdateCard;
using DATN_BE.Modules.Task.Application.Queries.GetCardById;
using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Modules.Workspaces.Application.Commands.DeleteWorkspace;
using DATN_BE.Modules.Workspaces.Application.Commands.CreateWorkspace;
using DATN_BE.Modules.Workspaces.Application.Commands.RemoveMemberFromProject;
using DATN_BE.Modules.Workspaces.Application.Queries.GetWorkspaceAuditLogs;
using DATN_BE.Modules.Workspaces.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using DATN_BE.Shared.Infrastructure.Persistence;
using DATN_BE.Shared.Infrastructure.Services;
using DATN_BE.Shared.Application.Behaviors;
using DATN_BE.Shared.Presentation.Middlewares;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DATN_BE.Tests;

public class WorkspaceProjectSecurityTests
{
    [Theory]
    [InlineData("member", true)]
    [InlineData("leader", true)]
    [InlineData("owner", false)]
    [InlineData("outsider", false)]
    public async Task Delete_column_requires_project_membership_and_cascades(string actor, bool allowed)
    {
        await using var f = await Fixture.Create();
        f.Current.UserId = f.Actor(actor);
        var oldDate = DateTimeOffset.UtcNow.AddDays(-2);
        var oldCard = new Card { ColumnId = f.Column.Id, Title = "Old", ReporterId = f.Leader.Id,
            IsDeleted = true, DeletedAt = oldDate };
        var otherColumn = new Column { BoardId = f.Board.Id, Title = "Other" };
        var otherCard = new Card { ColumnId = otherColumn.Id, Title = "Other", ReporterId = f.Leader.Id };
        f.Db.Columns.Add(otherColumn);
        f.Db.Cards.AddRange(oldCard, otherCard);
        await f.Db.SaveChangesAsync();
        var handler = new DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteColumn.DeleteColumnCommandHandler(f.Db, f.Auth);
        var command = new DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteColumn.DeleteColumnCommand
            { BoardId = f.Board.Id, ColumnId = f.Column.Id };
        if (!allowed)
        {
            await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, default));
            Assert.False((await f.Db.Columns.SingleAsync(c => c.Id == f.Column.Id)).IsDeleted);
            Assert.False((await f.Db.Cards.SingleAsync(c => c.Id == f.Card.Id)).IsDeleted);
            return;
        }
        Assert.True(await handler.Handle(command, default));
        f.Db.ChangeTracker.Clear();
        Assert.False(await f.Db.Columns.AnyAsync(c => c.Id == f.Column.Id));
        Assert.False(await f.Db.Cards.AnyAsync(c => c.Id == f.Card.Id));
        var column = await f.Db.Columns.IgnoreQueryFilters().SingleAsync(c => c.Id == f.Column.Id);
        Assert.Equal(column.DeletedAt, (await f.Db.Cards.IgnoreQueryFilters().SingleAsync(c => c.Id == f.Card.Id)).DeletedAt);
        Assert.Equal(oldDate, (await f.Db.Cards.IgnoreQueryFilters().SingleAsync(c => c.Id == oldCard.Id)).DeletedAt);
        Assert.True(await f.Db.Cards.AnyAsync(c => c.Id == otherCard.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, default));
    }

    [Fact]
    public async Task Delete_column_rejects_wrong_board_and_empty_ids()
    {
        await using var f = await Fixture.Create();
        f.Current.UserId = f.Member.Id;
        var board = new Board { ProjectId = f.Project.Id, Title = "Other", Slug = "other" };
        f.Db.Boards.Add(board);
        await f.Db.SaveChangesAsync();
        var handler = new DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteColumn.DeleteColumnCommandHandler(f.Db, f.Auth);
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new() { BoardId = board.Id, ColumnId = f.Column.Id }, default));
        var validator = new DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteColumn.DeleteColumnCommandValidator();
        Assert.False(validator.Validate(new DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteColumn.DeleteColumnCommand { ColumnId = f.Column.Id }).IsValid);
        Assert.False(validator.Validate(new DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteColumn.DeleteColumnCommand { BoardId = f.Board.Id }).IsValid);
        Assert.True(await f.Db.Cards.AnyAsync(c => c.Id == f.Card.Id));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_column_persists_fields_and_preserves_position(bool isDone)
    {
        await using var f = await Fixture.Create();
        f.Current.UserId = f.Member.Id;
        var original = await f.Db.Columns.SingleAsync(c => c.Id == f.Column.Id);
        original.IsDoneColumn = !isDone;
        original.Position = 7;
        await f.Db.SaveChangesAsync();
        var handler = new DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateColumn.UpdateColumnCommandHandler(f.Db, f.Auth);
        var result = await handler.Handle(new()
        {
            BoardId = f.Board.Id, ColumnId = f.Column.Id, Title = "  Updated  ", IsDoneColumn = isDone
        }, default);
        f.Db.ChangeTracker.Clear();
        var saved = await f.Db.Columns.SingleAsync(c => c.Id == result.Id);
        Assert.Equal("Updated", saved.Title);
        Assert.Equal(isDone, saved.IsDoneColumn);
        Assert.Equal(7, saved.Position);
        Assert.Equal(f.Board.Id, result.BoardId);
        Assert.Equal(saved.Title, result.Title);
        Assert.Equal(saved.IsDoneColumn, result.IsDoneColumn);
    }

    [Fact]
    public async Task Update_column_rejects_outsider_and_wrong_board()
    {
        await using var f = await Fixture.Create();
        var handler = new DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateColumn.UpdateColumnCommandHandler(f.Db, f.Auth);
        var command = new DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateColumn.UpdateColumnCommand
        {
            BoardId = f.Board.Id, ColumnId = f.Column.Id, Title = "Updated", IsDoneColumn = true
        };
        f.Current.UserId = f.Outsider.Id;
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, default));
        f.Current.UserId = f.Member.Id;
        var otherBoard = new Board { ProjectId = f.Project.Id, Title = "Other", Slug = "other" };
        f.Db.Boards.Add(otherBoard);
        await f.Db.SaveChangesAsync();
        command.BoardId = otherBoard.Id;
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, default));
        Assert.Equal("Column", f.Column.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Update_column_validates_title(string? title)
    {
        var validator = new DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateColumn.UpdateColumnCommandValidator();
        var command = new DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateColumn.UpdateColumnCommand
        {
            BoardId = Guid.NewGuid(), ColumnId = Guid.NewGuid(), Title = title!, IsDoneColumn = false
        };
        Assert.False(validator.Validate(command).IsValid);
        command.Title = new string('a', 101);
        Assert.False(validator.Validate(command).IsValid);
    }

    [Fact]
    public async Task My_boards_limit_each_group_and_isolate_current_user()
    {
        await using var f = await Fixture.Create();
        f.Current.UserId = f.Member.Id;
        var start = DateTimeOffset.UtcNow.AddDays(-1);
        var boards = Enumerable.Range(0, 6).Select(i => new Board
        {
            ProjectId = f.Project.Id, Title = $"Board {i}", Slug = $"board-{i}"
        }).ToArray();
        f.Db.Boards.AddRange(boards);
        for (var i = 0; i < boards.Length; i++)
            f.Db.UserBoardPreferences.Add(new UserBoardPreference
            {
                UserId = f.Member.Id, BoardId = boards[i].Id,
                IsPinned = i < 5, LastViewedAt = start.AddMinutes(i)
            });
        f.Db.UserBoardPreferences.Add(new UserBoardPreference
        {
            UserId = f.Leader.Id, BoardId = f.Board.Id, IsPinned = true,
            LastViewedAt = start.AddDays(2)
        });
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();
        var handler = new DATN_BE.Modules.ProjectManagement.Application.Queries.GetMyBoards.GetMyBoardsQueryHandler(f.Db, f.Current);
        var result = await handler.Handle(new(), default);
        Assert.Equal(new[] { boards[5].Id, boards[4].Id, boards[3].Id }, result.RecentBoards.Select(b => b.BoardId));
        Assert.Equal(new[] { boards[4].Id, boards[3].Id, boards[2].Id }, result.PinnedBoards.Select(b => b.BoardId));
        Assert.Equal(boards[5].Slug, result.RecentBoards[0].BoardSlug);
        Assert.All(result.RecentBoards, b => Assert.Equal(f.Project.Id, b.ProjectId));

        f.Current.UserId = f.Outsider.Id;
        Assert.Empty((await handler.Handle(new(), default)).RecentBoards);
        f.Current.UserId = null;
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new(), default));
    }

    [Theory]
    [InlineData("board")]
    [InlineData("project")]
    [InlineData("workspace")]
    [InlineData("membership")]
    public async Task My_boards_exclude_deleted_hierarchy_and_revoked_access(string removed)
    {
        await using var f = await Fixture.Create();
        f.Current.UserId = f.Member.Id;
        f.Db.UserBoardPreferences.Add(new UserBoardPreference
        {
            UserId = f.Member.Id, BoardId = f.Board.Id, IsPinned = true
        });
        await f.Db.SaveChangesAsync();
        if (removed == "board") f.Db.Boards.Remove(await f.Db.Boards.SingleAsync());
        if (removed == "project") f.Db.Projects.Remove(await f.Db.Projects.SingleAsync());
        if (removed == "workspace") f.Db.Workspaces.Remove(await f.Db.Workspaces.SingleAsync());
        if (removed == "membership") f.Db.ProjectMembers.Remove(await f.Db.ProjectMembers.SingleAsync(m => m.UserId == f.Member.Id));
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();
        var handler = new DATN_BE.Modules.ProjectManagement.Application.Queries.GetMyBoards.GetMyBoardsQueryHandler(f.Db, f.Current);
        var result = await handler.Handle(new(), default);
        Assert.Empty(result.RecentBoards);
        Assert.Empty(result.PinnedBoards);
    }

    [Theory]
    [InlineData("workspace", "owner", 1, 20, 200)]
    [InlineData("workspace", "leader", 1, 20, 403)]
    [InlineData("workspace", "outsider", 1, 20, 403)]
    [InlineData("workspace", "anonymous", 1, 20, 401)]
    [InlineData("workspace", "owner", 0, 20, 400)]
    [InlineData("project", "owner", 1, 20, 200)]
    [InlineData("project", "leader", 1, 20, 200)]
    [InlineData("project", "member", 1, 20, 403)]
    [InlineData("project", "outsider", 1, 20, 403)]
    [InlineData("project", "anonymous", 1, 20, 401)]
    [InlineData("project", "owner", 1, 101, 400)]
    public async Task Audit_HTTP_routes_enforce_authentication_authorization_and_validation(
        string resource, string actor, int pageIndex, int pageSize, int expectedStatus)
    {
        await using var f = await Fixture.Create();
        f.Current.UserId = actor == "anonymous" ? null : f.Actor(actor);
        using var host = await new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IApplicationDbContext>(f.Db);
                services.AddSingleton<ICurrentUserService>(f.Current);
                services.AddScoped<IProjectAuthorizationService, ProjectAuthorizationService>();
                services.AddControllers().AddApplicationPart(typeof(ProjectController).Assembly);
                services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
                services.AddAuthorization();
                services.AddMediatR(config =>
                {
                    config.RegisterServicesFromAssembly(typeof(ProjectController).Assembly);
                    config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
                });
                services.AddValidatorsFromAssembly(typeof(ProjectController).Assembly);
            })
            .Configure(app =>
            {
                app.UseMiddleware<ExceptionHandlingMiddleware>();
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            })).StartAsync();
        var route = resource == "workspace" ? $"Workspaces/{f.Workspace.Id}" : $"Project/{f.Project.Id}";
        using var client = host.GetTestClient();
        using var response = await client.GetAsync($"/api/{route}/audit-logs?pageIndex={pageIndex}&pageSize={pageSize}");
        Assert.Equal(expectedStatus, (int)response.StatusCode);
        if (expectedStatus == 200)
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(pageIndex, json.RootElement.GetProperty("data").GetProperty("pageIndex").GetInt32());
            Assert.Equal(JsonValueKind.Array, json.RootElement.GetProperty("data").GetProperty("items").ValueKind);
        }
    }

    [Theory]
    [InlineData("owner", true, false, false, true)]
    [InlineData("leader", true, true, true, true)]
    [InlineData("member", true, true, false, false)]
    [InlineData("outsider", false, false, false, false)]
    public async Task Policies_respect_workspace_ownership_and_direct_project_role(
        string actor, bool view, bool write, bool lead, bool manage)
    {
        await using var f = await Fixture.Create();
        f.Current.UserId = f.Actor(actor);
        await Check(() => f.Auth.EnsureCanViewProjectAsync(f.Project.Id), view);
        await Check(() => f.Auth.EnsureCanModifyProjectAsync(f.Project.Id), write);
        await Check(() => f.Auth.EnsureProjectLeaderAsync(f.Project.Id), lead);
        await Check(() => f.Auth.EnsureCanManageProjectAsync(f.Project.Id), manage);
    }

    [Fact]
    public async Task Owner_can_read_board_columns_cards_without_joining_project()
    {
        await using var f = await Fixture.Create();
        f.Current.UserId = f.Owner.Id;
        Assert.False(await f.Db.ProjectMembers.AnyAsync(m => m.UserId == f.Owner.Id));
        Assert.Single(await new GetProjectBoardsQueryHandler(f.Db, f.Current)
            .Handle(new(f.Project.Id), default));
        var detail = await new GetBoardDetailQueryHandler(f.Db, f.Auth)
            .Handle(new(f.Board.Slug, f.Project.Id), default);
        Assert.Single(detail.Columns);
        var card = await new GetCardByIdQueryHandler(f.Db, f.Auth).Handle(new(f.Card.Id), default);
        Assert.Equal(f.Card.Id, card.Id);
    }

    [Fact]
    public async Task Owner_outside_project_is_rejected_by_all_existing_data_write_handlers()
    {
        await using var f = await Fixture.Create();
        f.Current.UserId = f.Owner.Id;
        var writes = new Func<Task>[]
        {
            () => new CreateBoardCommandHandler(f.Db, f.Current, f.Auth).Handle(new() { ProjectId = f.Project.Id }, default),
            () => new UpdateBoardCommandHandler(f.Db, f.Current, f.Auth).Handle(new() { BoardId = f.Board.Id }, default),
            () => new DeleteBoardCommandHandler(f.Db, f.Current, f.Auth).Handle(new() { BoardId = f.Board.Id }, default),
            () => new CreateColumnCommandHandler(f.Db, f.Auth).Handle(new() { BoardId = f.Board.Id }, default),
            () => new ReorderColumnsCommandHandler(f.Db, f.Auth, null!, f.Current,
                NullLogger<ReorderColumnsCommandHandler>.Instance).Handle(
                    new() { BoardId = f.Board.Id, OrderedColumnIds = new() { f.Column.Id } }, default),
            () => new CreateCardCommandHandler(f.Db, f.Current, f.Auth).Handle(new() { ColumnId = f.Column.Id }, default),
            () => new UpdateCardCommandHandler(f.Db, f.Auth).Handle(new() { CardId = f.Card.Id }, default),
            () => new DeleteCardCommandHandler(f.Db, f.Auth).Handle(new(f.Card.Id), default),
            () => new MoveCardCommandHandler(f.Db, f.Auth).Handle(new() { CardId = f.Card.Id }, default),
            () => new AddCardCommentCommandHandler(f.Db, f.Current, f.Auth).Handle(new() { CardId = f.Card.Id }, default)
        };
        foreach (var write in writes)
            await Assert.ThrowsAsync<ForbiddenException>(write);
        Assert.Empty(await f.Db.ActivityLogs.ToListAsync());
        Assert.Equal("Card", (await f.Db.Cards.SingleAsync()).Title);
        Assert.Single(await f.Db.Boards.ToListAsync());
    }

    [Fact]
    public async Task Owner_who_joins_as_member_can_write_cards_but_cannot_manage_boards()
    {
        await using var f = await Fixture.Create();
        f.Db.ProjectMembers.Add(new() { ProjectId = f.Project.Id, UserId = f.Owner.Id, RoleId = f.MemberRole.Id });
        await f.Db.SaveChangesAsync();
        f.Current.UserId = f.Owner.Id;
        await f.Auth.EnsureCanModifyProjectAsync(f.Project.Id);
        await Assert.ThrowsAsync<ForbiddenException>(() => f.Auth.EnsureProjectLeaderAsync(f.Project.Id));
        var member = await f.Db.ProjectMembers.SingleAsync(m => m.UserId == f.Owner.Id);
        member.RoleId = f.LeaderRole.Id;
        await f.Db.SaveChangesAsync();
        await f.Auth.EnsureProjectLeaderAsync(f.Project.Id);
    }

    [Fact]
    public async Task Project_leader_can_create_update_delete_board_and_member_can_update_card()
    {
        await using var f = await Fixture.Create();
        f.Current.UserId = f.Leader.Id;
        var board = await new CreateBoardCommandHandler(f.Db, f.Current, f.Auth)
            .Handle(new() { ProjectId = f.Project.Id, Title = "New board", Slug = "new-board" }, default);
        var updated = await new UpdateBoardCommandHandler(f.Db, f.Current, f.Auth)
            .Handle(new() { BoardId = board.Id, Title = "Changed", Slug = "changed" }, default);
        Assert.Equal("Changed", updated.Title);
        await new DeleteBoardCommandHandler(f.Db, f.Current, f.Auth).Handle(new() { BoardId = board.Id }, default);
        Assert.False(await f.Db.Boards.AnyAsync(b => b.Id == board.Id));
        f.Current.UserId = f.Member.Id;
        var card = await new UpdateCardCommandHandler(f.Db, f.Auth)
            .Handle(new() { CardId = f.Card.Id, Title = "Updated card", AssigneeId = f.Member.Id }, default);
        Assert.Equal("Updated card", card.Title);
    }

    [Fact]
    public async Task Leader_role_from_another_project_or_scope_cannot_manage_this_project()
    {
        await using var f = await Fixture.Create();
        var otherProject = new Project { Name = "Other", ProjectKey = "OTHER", WorkspaceId = f.Workspace.Id, CreatedById = f.Owner.Id };
        f.Db.Projects.Add(otherProject);
        f.Db.ProjectMembers.Add(new() { ProjectId = otherProject.Id, UserId = f.Outsider.Id, RoleId = f.LeaderRole.Id });
        await f.Db.SaveChangesAsync();
        f.Current.UserId = f.Outsider.Id;
        await Assert.ThrowsAsync<ForbiddenException>(() => f.Auth.EnsureProjectLeaderAsync(f.Project.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => f.Auth.EnsureCanManageProjectAsync(f.Project.Id));
        f.Current.UserId = f.Leader.Id;
        f.Db.Roles.Update(f.LeaderRole);
        f.LeaderRole.Scope = RoleScope.Workspace;
        await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenException>(() => f.Auth.EnsureProjectLeaderAsync(f.Project.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => f.Auth.EnsureCanManageProjectAsync(f.Project.Id));
    }

    [Fact]
    public async Task Workspace_and_project_creation_and_project_deletion_are_audited()
    {
        await using var f = await Fixture.Create();
        f.Db.Roles.Add(new() { Name = RoleConstants.WorkspaceOwner, Scope = RoleScope.Workspace });
        await f.Db.SaveChangesAsync();
        f.Current.UserId = f.Owner.Id;
        var workspace = await new CreateWorkspaceCommandHandler(f.Db, f.Current)
            .Handle(new() { Name = "Created workspace" }, default);
        var project = await new CreateProjectCommandHandler(f.Db, f.Current)
            .Handle(new() { WorkspaceId = workspace.Id, Name = "Created project", ProjectKey = "NEW", IsAssignAsLeader = false }, default);
        Assert.False(await f.Db.ProjectMembers.AnyAsync(m => m.ProjectId == project.Id && m.UserId == f.Owner.Id));
        await new DeleteProjectCommandHandler(f.Db, f.Auth).Handle(new() { ProjectId = project.Id }, default);
        var logs = await f.Db.ActivityLogs.ToListAsync();
        Assert.Contains(logs, log => log.EntityId == workspace.Id && log.ActionType == ActivityActionType.Created);
        Assert.Contains(logs, log => log.EntityId == workspace.Id && log.ActionType == ActivityActionType.MemberAdded);
        Assert.Contains(logs, log => log.EntityId == project.Id && log.ActionType == ActivityActionType.Created);
        Assert.Contains(logs, log => log.EntityId == project.Id && log.ActionType == ActivityActionType.Deleted);
    }

    [Fact]
    public void Audit_query_validation_bounds_pagination()
    {
        Assert.False(new GetProjectAuditLogsQueryValidator().Validate(new GetProjectAuditLogsQuery(Guid.Empty, 0, 101)).IsValid);
        Assert.False(new GetWorkspaceAuditLogsQueryValidator().Validate(new GetWorkspaceAuditLogsQuery(Guid.Empty, int.MaxValue, 0)).IsValid);
        Assert.True(new GetProjectAuditLogsQueryValidator().Validate(new GetProjectAuditLogsQuery(Guid.NewGuid(), 1, 100)).IsValid);
    }

    [Fact]
    public async Task Project_update_by_owner_logs_actor_and_before_after_without_audit_on_noop()
    {
        await using var f = await Fixture.Create();
        f.Current.UserId = f.Owner.Id;
        var handler = new UpdateProjectCommandHandler(f.Db, f.Auth, f.Current);
        var command = new UpdateProjectCommand { ProjectId = f.Project.Id, Name = "Renamed", ProjectKey = "PRJ" };
        await handler.Handle(command, default);
        var log = await f.Db.ActivityLogs.SingleAsync();
        Assert.Equal(f.Owner.Id, log.UserId);
        Assert.Equal(ActivityActionType.Updated, log.ActionType);
        using var details = JsonDocument.Parse(log.DetailsJson!);
        Assert.Equal("Project", details.RootElement.GetProperty("before").GetProperty("name").GetString());
        Assert.Equal("Renamed", details.RootElement.GetProperty("after").GetProperty("name").GetString());
        await handler.Handle(command, default);
        Assert.Equal(1, await f.Db.ActivityLogs.CountAsync());
    }

    [Fact]
    public async Task Leader_transfer_logs_both_role_changes()
    {
        await using var f = await Fixture.Create();
        f.Current.UserId = f.Owner.Id;
        await new UpdateProjectMemberRoleCommandHandler(f.Db, f.Current).Handle(
            new() { ProjectId = f.Project.Id, UserId = f.Member.Id, NewRoleId = f.LeaderRole.Id }, default);
        var logs = await f.Db.ActivityLogs.ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.All(logs, log => Assert.Equal(ActivityActionType.RoleChanged, log.ActionType));
        Assert.All(logs, log => Assert.Equal(f.Project.Id, log.EntityId));
        Assert.All(logs, log => Assert.Contains("memberUserId", log.DetailsJson));
    }

    [Fact]
    public async Task Removing_workspace_member_audits_each_project_membership_cleanup()
    {
        await using var f = await Fixture.Create();
        f.Current.UserId = f.Owner.Id;
        await new RemoveMemberFromWorkspaceCommandHandler(f.Db, f.Current)
            .Handle(new(f.Workspace.Id, f.Member.Id), default);
        var logs = await f.Db.ActivityLogs.ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.All(logs, log => Assert.Equal(ActivityActionType.MemberRemoved, log.ActionType));
        Assert.Contains(logs, log => log.EntityType == ActivityEntityType.Workspace);
        Assert.Contains(logs, log => log.EntityType == ActivityEntityType.Project);
        Assert.Null((await f.Db.Cards.SingleAsync()).AssigneeId);
    }

    [Fact]
    public async Task Create_membership_and_remove_project_member_are_audited()
    {
        await using var f = await Fixture.Create();
        f.Current.UserId = f.Owner.Id;
        f.Db.ProjectMembers.Add(new() { ProjectId = f.Project.Id, UserId = f.Owner.Id, RoleId = f.MemberRole.Id });
        f.Db.SaveChanges();
        Assert.Equal(ActivityActionType.MemberAdded, (await f.Db.ActivityLogs.SingleAsync()).ActionType);
        await new RemoveMemberFormProjectCommandHandler(f.Db, f.Current).Handle(new(f.Project.Id, f.Owner.Id), default);
        Assert.Equal(2, await f.Db.ActivityLogs.CountAsync());
        Assert.Contains(await f.Db.ActivityLogs.ToListAsync(), log => log.ActionType == ActivityActionType.MemberRemoved);
    }

    [Fact]
    public async Task Workspace_cascade_is_audited_and_deleted_history_remains_authorized()
    {
        await using var f = await Fixture.Create();
        f.Current.UserId = f.Owner.Id;
        var priorDeletedAt = DateTimeOffset.UtcNow.AddDays(-3);
        f.Db.Boards.Add(new() { ProjectId = f.Project.Id, Title = "Old", Slug = "old", IsDeleted = true, DeletedAt = priorDeletedAt });
        await f.Db.SaveChangesAsync();
        await new DeleteWorkspaceCommandHandler(f.Db, f.Current).Handle(new(f.Workspace.Id), default);
        Assert.Empty(await f.Db.Workspaces.ToListAsync());
        Assert.Empty(await f.Db.Cards.ToListAsync());
        Assert.Equal(priorDeletedAt, (await f.Db.Boards.IgnoreQueryFilters().SingleAsync(b => b.Slug == "old")).DeletedAt);
        var projectPage = await new GetProjectAuditLogsQueryHandler(f.Db, f.Auth).Handle(new(f.Project.Id), default);
        Assert.Single(projectPage.Items);
        Assert.Equal("Deleted", projectPage.Items[0].ActionType);
        var workspacePage = await new GetWorkspaceAuditLogsQueryHandler(f.Db, f.Current).Handle(new(f.Workspace.Id), default);
        Assert.Single(workspacePage.Items);
        f.Current.UserId = f.Outsider.Id;
        await Assert.ThrowsAsync<ForbiddenException>(() => new GetWorkspaceAuditLogsQueryHandler(f.Db, f.Current).Handle(new(f.Workspace.Id), default));
        await Assert.ThrowsAsync<ForbiddenException>(() => new GetProjectAuditLogsQueryHandler(f.Db, f.Auth).Handle(new(f.Project.Id), default));
    }

    [Fact]
    public async Task Audit_pagination_is_scoped_and_keeps_deleted_actor_name()
    {
        await using var f = await Fixture.Create();
        f.Current.UserId = f.Owner.Id;
        var project = await f.Db.Projects.SingleAsync();
        project.Name = "First";
        await f.Db.SaveChangesAsync();
        project.Name = "Second";
        await f.Db.SaveChangesAsync();
        var workspace = await f.Db.Workspaces.SingleAsync();
        workspace.Description = "Changed";
        await f.Db.SaveChangesAsync();
        var owner = await f.Db.Users.SingleAsync(u => u.Id == f.Owner.Id);
        owner.IsDeleted = true;
        await f.Db.SaveChangesAsync();
        var page = await new GetProjectAuditLogsQueryHandler(f.Db, f.Auth).Handle(new(f.Project.Id, 1, 1), default);
        Assert.Equal(2, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("owner", page.Items[0].Username);
        Assert.True(page.HasNextPage);
        Assert.Equal(1, (await new GetWorkspaceAuditLogsQueryHandler(f.Db, f.Current).Handle(new(f.Workspace.Id), default)).TotalCount);
        f.Current.UserId = f.Member.Id;
        await Assert.ThrowsAsync<ForbiddenException>(() => new GetProjectAuditLogsQueryHandler(f.Db, f.Auth).Handle(new(f.Project.Id), default));
    }

    [Fact]
    public async Task Failed_save_does_not_persist_audit_and_retry_writes_one_row()
    {
        var interceptor = new FailOnceInterceptor();
        await using var f = await Fixture.Create(interceptor);
        f.Current.UserId = f.Owner.Id;
        (await f.Db.Projects.SingleAsync()).Name = "Changed";
        interceptor.FailNext = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync());
        Assert.Empty(f.Db.ChangeTracker.Entries<ActivityLog>());
        Assert.Equal(0, await f.Db.ActivityLogs.CountAsync());
        await f.Db.SaveChangesAsync();
        Assert.Equal(1, await f.Db.ActivityLogs.CountAsync());
    }

    [Theory]
    [InlineData("member", true)]
    [InlineData("leader", true)]
    [InlineData("owner", false)]
    [InlineData("outsider", false)]
    public async Task Board_preferences_require_direct_project_membership(string actor, bool allowed)
    {
        await using var f = await Fixture.Create();
        f.Current.UserId = f.Actor(actor);
        var handler = new DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateBoardPreference.UpdateBoardPreferenceCommandHandler(f.Db, f.Current, f.Auth);
        var command = new DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateBoardPreference.UpdateBoardPreferenceCommand
            { BoardId = f.Board.Id, IsPinned = true, MarkViewed = true };
        if (!allowed)
        {
            await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, default));
            Assert.Empty(await f.Db.UserBoardPreferences.ToListAsync());
            return;
        }
        var result = await handler.Handle(command, default);
        Assert.True(result.IsPinned);
        var viewedAt = result.LastViewedAt;
        command.IsPinned = false;
        command.MarkViewed = false;
        result = await handler.Handle(command, default);
        Assert.False(result.IsPinned);
        Assert.Equal(viewedAt, result.LastViewedAt);
        command.IsPinned = null;
        command.MarkViewed = true;
        result = await handler.Handle(command, default);
        Assert.False(result.IsPinned);
        Assert.True(result.LastViewedAt >= viewedAt);
        Assert.Single(await f.Db.UserBoardPreferences.ToListAsync());
    }

    [Theory]
    [InlineData("board")]
    [InlineData("project")]
    [InlineData("workspace")]
    public async Task Board_preferences_reject_deleted_hierarchy(string deleted)
    {
        await using var f = await Fixture.Create();
        f.Current.UserId = f.Member.Id;
        if (deleted == "board") (await f.Db.Boards.SingleAsync()).IsDeleted = true;
        if (deleted == "project") (await f.Db.Projects.SingleAsync()).IsDeleted = true;
        if (deleted == "workspace") (await f.Db.Workspaces.SingleAsync()).IsDeleted = true;
        await f.Db.SaveChangesAsync();
        var handler = new DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateBoardPreference.UpdateBoardPreferenceCommandHandler(f.Db, f.Current, f.Auth);
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new() { BoardId = f.Board.Id, MarkViewed = true }, default));
        Assert.Empty(await f.Db.UserBoardPreferences.ToListAsync());
    }

    private static async Task Check(Func<Task> action, bool allowed)
    {
        if (allowed) await action();
        else await Assert.ThrowsAsync<ForbiddenException>(action);
    }

    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId { get; set; }
        public string? Email => "test@example.com";
        public string? Username => "test";
        public bool IsAuthenticated => UserId.HasValue;
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
        ICurrentUserService currentUser) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (currentUser.UserId is not Guid userId)
                return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }

    private sealed class FailOnceInterceptor : SaveChangesInterceptor
    {
        public bool FailNext { get; set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (FailNext)
            {
                FailNext = false;
                throw new InvalidOperationException("Injected save failure");
            }
            return ValueTask.FromResult(result);
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public CurrentUser Current { get; } = new();
        public ApplicationDbContext Db { get; private set; } = null!;
        public ProjectAuthorizationService Auth => new(Db, Current);
        public User Owner { get; } = new() { Username = "owner" };
        public User Leader { get; } = new() { Username = "leader" };
        public User Member { get; } = new() { Username = "member" };
        public User Outsider { get; } = new() { Username = "outsider" };
        public Role LeaderRole { get; } = new() { Name = RoleConstants.ProjectLeader, Scope = RoleScope.Project };
        public Role MemberRole { get; } = new() { Name = RoleConstants.ProjectMember, Scope = RoleScope.Project };
        public Workspace Workspace { get; private set; } = null!;
        public Project Project { get; private set; } = null!;
        public Board Board { get; private set; } = null!;
        public Column Column { get; private set; } = null!;
        public Card Card { get; private set; } = null!;
        public Guid Actor(string actor) => actor switch
        {
            "owner" => Owner.Id, "leader" => Leader.Id, "member" => Member.Id, _ => Outsider.Id
        };

        public static async Task<Fixture> Create(params IInterceptor[] interceptors)
        {
            var f = new Fixture();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).AddInterceptors(interceptors).Options;
            f.Db = new(options, f.Current);
            f.Db.Roles.AddRange(f.LeaderRole, f.MemberRole);
            foreach (var user in new[] { f.Owner, f.Leader, f.Member, f.Outsider })
            {
                user.RoleId = f.MemberRole.Id;
                user.Email = user.Username + "@example.com";
                f.Db.Users.Add(user);
            }
            f.Workspace = new() { Name = "Workspace", Slug = "workspace", OwnerId = f.Owner.Id };
            f.Db.Workspaces.Add(f.Workspace);
            f.Project = new() { Name = "Project", ProjectKey = "PRJ", WorkspaceId = f.Workspace.Id, CreatedById = f.Owner.Id };
            f.Db.Projects.Add(f.Project);
            f.Db.WorkspaceMembers.AddRange(
                new() { WorkspaceId = f.Workspace.Id, UserId = f.Leader.Id, RoleId = f.MemberRole.Id },
                new() { WorkspaceId = f.Workspace.Id, UserId = f.Member.Id, RoleId = f.MemberRole.Id });
            f.Db.ProjectMembers.AddRange(
                new() { ProjectId = f.Project.Id, UserId = f.Leader.Id, RoleId = f.LeaderRole.Id },
                new() { ProjectId = f.Project.Id, UserId = f.Member.Id, RoleId = f.MemberRole.Id });
            f.Board = new() { ProjectId = f.Project.Id, Title = "Board", Slug = "board" };
            f.Db.Boards.Add(f.Board);
            f.Column = new() { BoardId = f.Board.Id, Title = "Column" };
            f.Db.Columns.Add(f.Column);
            f.Card = new() { ColumnId = f.Column.Id, Title = "Card", ReporterId = f.Leader.Id, AssigneeId = f.Member.Id };
            f.Db.Cards.Add(f.Card);
            await f.Db.SaveChangesAsync();
            f.Db.ChangeTracker.Clear();
            return f;
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}
