using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Modules.Task.Application.Commands.AddCardDependency;
using DATN_BE.Modules.Task.Application.Queries.SearchCardCandidates;
using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Modules.Task.Domain.Enum;
using DATN_BE.Modules.Workspaces.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using DATN_BE.Shared.Infrastructure.Persistence;
using DATN_BE.Shared.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DATN_BE.Tests;

public class CardDependencyTests
{
    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }
        public bool IsAuthenticated => UserId.HasValue;
    }

    private static async Task<(
        ApplicationDbContext Db,
        CurrentUser Current,
        ProjectAuthorizationService Auth,
        Board ScrumBoard,
        Board KanbanBoard,
        Sprint Sprint1,
        Sprint Sprint2,
        Card CardSprint1A,
        Card CardSprint1B,
        Card CardSprint2,
        Card CardBacklog,
        Card KanbanCard1,
        Card KanbanCard2)> SetupAsync()
    {
        var current = new CurrentUser();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new ApplicationDbContext(options, current);

        var memberRole = new Role { Name = RoleConstants.ProjectMember, Scope = RoleScope.Project };
        db.Roles.Add(memberRole);

        var user = new User { Username = "tester", Email = "tester@test.com", RoleId = memberRole.Id };
        db.Users.Add(user);
        current.UserId = user.Id;

        var workspace = new Workspace { Name = "Workspace", Slug = "ws", OwnerId = user.Id };
        db.Workspaces.Add(workspace);

        var project = new Project { Name = "Project", ProjectKey = "PRJ", WorkspaceId = workspace.Id, CreatedById = user.Id };
        db.Projects.Add(project);

        db.WorkspaceMembers.Add(new WorkspaceMember { WorkspaceId = workspace.Id, UserId = user.Id, RoleId = memberRole.Id });
        db.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = user.Id, RoleId = memberRole.Id });

        // Scrum Board
        var scrumBoard = new Board { ProjectId = project.Id, Title = "Scrum Board", Slug = "scrum", Type = BoardType.Scrum };
        db.Boards.Add(scrumBoard);
        var scrumCol = new Column { BoardId = scrumBoard.Id, Title = "To Do", Position = 0 };
        db.Columns.Add(scrumCol);

        var sprint1 = new Sprint { ProjectId = project.Id, BoardId = scrumBoard.Id, Name = "Sprint 1", Status = SprintStatus.Active };
        var sprint2 = new Sprint { ProjectId = project.Id, BoardId = scrumBoard.Id, Name = "Sprint 2", Status = SprintStatus.Future };
        db.Sprints.AddRange(sprint1, sprint2);

        var cardS1A = new Card { Title = "Card S1 A", ColumnId = scrumCol.Id, SprintId = sprint1.Id, ReporterId = user.Id };
        var cardS1B = new Card { Title = "Card S1 B", ColumnId = scrumCol.Id, SprintId = sprint1.Id, ReporterId = user.Id };
        var cardS2 = new Card { Title = "Card S2", ColumnId = scrumCol.Id, SprintId = sprint2.Id, ReporterId = user.Id };
        var cardBacklog = new Card { Title = "Card Backlog", ColumnId = scrumCol.Id, SprintId = null, ReporterId = user.Id };
        db.Cards.AddRange(cardS1A, cardS1B, cardS2, cardBacklog);

        // Kanban Board
        var kanbanBoard = new Board { ProjectId = project.Id, Title = "Kanban Board", Slug = "kanban", Type = BoardType.Kanban };
        db.Boards.Add(kanbanBoard);
        var kanbanCol = new Column { BoardId = kanbanBoard.Id, Title = "To Do", Position = 0 };
        db.Columns.Add(kanbanCol);

        var kanbanCard1 = new Card { Title = "Kanban 1", ColumnId = kanbanCol.Id, SprintId = null, ReporterId = user.Id };
        var kanbanCard2 = new Card { Title = "Kanban 2", ColumnId = kanbanCol.Id, SprintId = null, ReporterId = user.Id };
        db.Cards.AddRange(kanbanCard1, kanbanCard2);

        await db.SaveChangesAsync();

        var auth = new ProjectAuthorizationService(db, current);
        return (db, current, auth, scrumBoard, kanbanBoard, sprint1, sprint2, cardS1A, cardS1B, cardS2, cardBacklog, kanbanCard1, kanbanCard2);
    }

    [Fact]
    public async Task SearchCandidates_InScrumBoard_OnlyReturnsCardsInSameSprint()
    {
        var f = await SetupAsync();
        var handler = new SearchCardCandidatesQueryHandler(f.Db, f.Auth);

        // Tìm candidate cho card ở Sprint 1
        var candidates = await handler.Handle(new SearchCardCandidatesQuery(f.CardSprint1A.Id, null), default);

        // Chỉ chứa Card S1 B, KHÔNG chứa Card S2 hay Card Backlog
        Assert.Single(candidates);
        Assert.Equal(f.CardSprint1B.Id, candidates[0].Id);
    }

    [Fact]
    public async Task SearchCandidates_InScrumBoard_ForBacklogCard_OnlyReturnsBacklogCards()
    {
        var f = await SetupAsync();
        var handler = new SearchCardCandidatesQueryHandler(f.Db, f.Auth);

        // Thêm 1 thẻ backlog thứ 2 để kiểm tra
        var cardBacklog2 = new Card { Title = "Card Backlog 2", ColumnId = f.CardBacklog.ColumnId, SprintId = null, ReporterId = f.CardBacklog.ReporterId };
        f.Db.Cards.Add(cardBacklog2);
        await f.Db.SaveChangesAsync();

        var candidates = await handler.Handle(new SearchCardCandidatesQuery(f.CardBacklog.Id, null), default);

        Assert.Single(candidates);
        Assert.Equal(cardBacklog2.Id, candidates[0].Id);
    }

    [Fact]
    public async Task SearchCandidates_InKanbanBoard_ReturnsAllCardsOnBoard()
    {
        var f = await SetupAsync();
        var handler = new SearchCardCandidatesQueryHandler(f.Db, f.Auth);

        var candidates = await handler.Handle(new SearchCardCandidatesQuery(f.KanbanCard1.Id, null), default);

        Assert.Single(candidates);
        Assert.Equal(f.KanbanCard2.Id, candidates[0].Id);
    }

    [Fact]
    public async Task AddCardDependency_InScrumBoard_SameSprint_Succeeds()
    {
        var f = await SetupAsync();
        var handler = new AddCardDependencyCommandHandler(f.Db, f.Auth, f.Current);

        var result = await handler.Handle(new AddCardDependencyCommand
        {
            CardId = f.CardSprint1A.Id,
            TargetCardId = f.CardSprint1B.Id,
            Direction = DependencyDirection.BlockedBy
        }, default);

        Assert.NotNull(result);
        Assert.Equal(f.CardSprint1B.Id, result.CardId);
    }

    [Fact]
    public async Task AddCardDependency_InScrumBoard_DifferentSprint_ThrowsBadRequestException()
    {
        var f = await SetupAsync();
        var handler = new AddCardDependencyCommandHandler(f.Db, f.Auth, f.Current);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new AddCardDependencyCommand
        {
            CardId = f.CardSprint1A.Id,
            TargetCardId = f.CardSprint2.Id,
            Direction = DependencyDirection.BlockedBy
        }, default));

        Assert.Contains("Trong bảng Scrum", ex.Message);
    }

    [Fact]
    public async Task AddCardDependency_InScrumBoard_SprintAndBacklog_ThrowsBadRequestException()
    {
        var f = await SetupAsync();
        var handler = new AddCardDependencyCommandHandler(f.Db, f.Auth, f.Current);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new AddCardDependencyCommand
        {
            CardId = f.CardSprint1A.Id,
            TargetCardId = f.CardBacklog.Id,
            Direction = DependencyDirection.BlockedBy
        }, default));

        Assert.Contains("Trong bảng Scrum", ex.Message);
    }

    [Fact]
    public async Task AddCardDependency_InKanbanBoard_Succeeds()
    {
        var f = await SetupAsync();
        var handler = new AddCardDependencyCommandHandler(f.Db, f.Auth, f.Current);

        var result = await handler.Handle(new AddCardDependencyCommand
        {
            CardId = f.KanbanCard1.Id,
            TargetCardId = f.KanbanCard2.Id,
            Direction = DependencyDirection.BlockedBy
        }, default);

        Assert.NotNull(result);
        Assert.Equal(f.KanbanCard2.Id, result.CardId);
    }
}
