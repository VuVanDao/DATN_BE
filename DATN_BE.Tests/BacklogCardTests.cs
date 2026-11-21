using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Modules.Task.Application.Commands.CreateBacklogCard;
using DATN_BE.Modules.Task.Domain.Enum;
using DATN_BE.Modules.Workspaces.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using DATN_BE.Shared.Infrastructure.Persistence;
using DATN_BE.Shared.Infrastructure.Services;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DATN_BE.Tests;

public class BacklogCardTests
{
    [Fact]
    public void Validator_rejects_empty_board_id()
    {
        var validator = new CreateBacklogCardCommandValidator();

        var result = validator.TestValidate(new CreateBacklogCardCommand
        {
            BoardId = Guid.Empty,
            Title = ""
        });

        result.ShouldHaveValidationErrorFor(x => x.BoardId);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public async Task Create_backlog_card_assigns_to_specified_scrum_board_and_first_column()
    {
        await using var f = await BacklogFixture.Create();
        f.Current.UserId = f.Owner.Id;

        var handler = new CreateBacklogCardCommandHandler(
            f.Db,
            f.Current,
            f.Auth);

        var result = await handler.Handle(new CreateBacklogCardCommand
        {
            BoardId = f.ScrumBoard.Id,
            Title = "Task in Backlog",
            StoryPoints = 5,
            Type = CardType.Features
        }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Task in Backlog", result.Title);
        Assert.Equal(f.ColumnToDo.Id, result.ColumnId);
        Assert.Null(result.SprintId);
        Assert.Equal(5, result.StoryPoints);

        var persisted = await f.Db.Cards.SingleAsync(c => c.Id == result.Id);
        Assert.Equal(f.ColumnToDo.Id, persisted.ColumnId);
    }

    [Fact]
    public async Task Create_backlog_card_with_wrong_or_non_scrum_board_throws_not_found()
    {
        await using var f = await BacklogFixture.Create();
        f.Current.UserId = f.Owner.Id;

        var handler = new CreateBacklogCardCommandHandler(
            f.Db,
            f.Current,
            f.Auth);

        // Kanban board thay vì Scrum board
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new CreateBacklogCardCommand
        {
            BoardId = f.KanbanBoard.Id,
            Title = "Invalid Board Task"
        }, CancellationToken.None));

        // Random Guid
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new CreateBacklogCardCommand
        {
            BoardId = Guid.NewGuid(),
            Title = "Non-existent Board Task"
        }, CancellationToken.None));
    }

    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId { get; set; }
        public string? Email => "owner@example.com";
        public string? Username => "owner";
        public bool IsAuthenticated => UserId.HasValue;
    }

    private sealed class BacklogFixture : IAsyncDisposable
    {
        public CurrentUser Current { get; } = new();
        public ApplicationDbContext Db { get; private set; } = null!;
        public ProjectAuthorizationService Auth => new(Db, Current);

        public User Owner { get; } = new() { Username = "owner", Email = "owner@example.com", IsActive = true };
        public Role SystemUserRole { get; } = new() { Name = RoleConstants.User, Scope = RoleScope.Global };
        public Role WsMemberRole { get; } = new() { Name = RoleConstants.WorkspaceMember, Scope = RoleScope.Workspace };
        public Role ProjectLeaderRole { get; } = new() { Name = RoleConstants.ProjectLeader, Scope = RoleScope.Project };

        public Workspace Workspace { get; private set; } = null!;
        public Project Project { get; private set; } = null!;
        public Board ScrumBoard { get; private set; } = null!;
        public Board KanbanBoard { get; private set; } = null!;
        public Column ColumnToDo { get; private set; } = null!;

        public static async Task<BacklogFixture> Create()
        {
            var f = new BacklogFixture();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            f.Db = new ApplicationDbContext(options, f.Current);
            f.Db.Roles.AddRange(f.SystemUserRole, f.WsMemberRole, f.ProjectLeaderRole);

            f.Owner.RoleId = f.SystemUserRole.Id;
            f.Db.Users.Add(f.Owner);

            f.Workspace = new Workspace
            {
                Name = "Workspace A",
                Slug = "workspace-a",
                OwnerId = f.Owner.Id
            };
            f.Db.Workspaces.Add(f.Workspace);

            f.Project = new Project
            {
                Name = "Project Alpha",
                ProjectKey = "ALPHA",
                WorkspaceId = f.Workspace.Id,
                CreatedById = f.Owner.Id
            };
            f.Db.Projects.Add(f.Project);

            f.Db.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = f.Project.Id,
                UserId = f.Owner.Id,
                RoleId = f.ProjectLeaderRole.Id
            });

            f.ScrumBoard = new Board
            {
                Title = "Scrum Board",
                Slug = "scrum-board",
                Type = BoardType.Scrum,
                ProjectId = f.Project.Id
            };
            f.KanbanBoard = new Board
            {
                Title = "Kanban Board",
                Slug = "kanban-board",
                Type = BoardType.Kanban,
                ProjectId = f.Project.Id
            };
            f.Db.Boards.AddRange(f.ScrumBoard, f.KanbanBoard);

            f.ColumnToDo = new Column
            {
                Title = "To Do",
                Position = 0,
                IsDoneColumn = false,
                BoardId = f.ScrumBoard.Id
            };
            f.Db.Columns.Add(f.ColumnToDo);

            await f.Db.SaveChangesAsync();
            return f;
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}
