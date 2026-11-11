using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Presentation.Hubs;

[Authorize]
public sealed class BoardHub : Hub
{
    public const string Route = "/hubs/board";
    public const string ColumnsReordered = "ColumnsReordered";
    public const string CardMoved = "CardMoved";
    public const string ColumnCreated = "ColumnCreated";
    public const string ColumnUpdated = "ColumnUpdated";
    public const string ColumnDeleted = "ColumnDeleted";
    public const string CardCreated = "CardCreated";
    public const string CardUpdated = "CardUpdated";
    public const string CardDeleted = "CardDeleted";
    public const string SprintCreated = "SprintCreated";
    public const string SprintStarted = "SprintStarted";
    public const string SprintUpdated = "SprintUpdated";
    public const string SprintCompleted = "SprintCompleted";
    public const string SprintDeleted = "SprintDeleted";
    public const string CardSprintChanged = "CardSprintChanged";
    public const string CardDependencyAdded = "CardDependencyAdded";
    public const string CardDependencyRemoved = "CardDependencyRemoved";

    private readonly IApplicationDbContext _context;
    private readonly IProjectAuthorizationService _authorizationService;

    public BoardHub(
        IApplicationDbContext context,
        IProjectAuthorizationService authorizationService)
    {
        _context = context;
        _authorizationService = authorizationService;
    }

    public static string GroupName(Guid boardId)
        => $"board:{boardId:N}";

    public async System.Threading.Tasks.Task JoinBoard(Guid boardId)
    {
        var projectId = await _context.Boards
            .AsNoTracking()
            .Where(board => board.Id == boardId)
            .Select(board => (Guid?)board.ProjectId)
            .SingleOrDefaultAsync(Context.ConnectionAborted);

        if (!projectId.HasValue)
        {
            throw new HubException("Không tìm thấy board.");
        }

        try
        {
            await _authorizationService.EnsureCanViewProjectAsync(
                projectId.Value,
                Context.ConnectionAborted);
        }
        catch (ForbiddenException exception)
        {
            throw new HubException(exception.Message);
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GroupName(boardId),
            Context.ConnectionAborted);
    }

    public System.Threading.Tasks.Task LeaveBoard(Guid boardId)
    {
        return Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            GroupName(boardId),
            Context.ConnectionAborted);
    }
}

public sealed record ColumnsReorderedMessage(
    Guid BoardId,
    IReadOnlyList<Guid> OrderedColumnIds,
    Guid ChangedByUserId,
    DateTimeOffset OccurredAt);

public sealed record CardMovedMessage(
    Guid BoardId,
    Guid CardId,
    Guid SourceColumnId,
    Guid TargetColumnId,
    int NewPosition,
    int ProgressPercentage,
    Guid? ChangedByUserId,
    DateTimeOffset OccurredAt);

public sealed record ColumnCreatedMessage(
    Guid BoardId,
    Guid ColumnId,
    string Title,
    int Position,
    bool IsDoneColumn,
    Guid ChangedByUserId,
    DateTimeOffset OccurredAt);

public sealed record ColumnUpdatedMessage(
    Guid BoardId,
    Guid ColumnId,
    string Title,
    int Position,
    bool IsDoneColumn,
    Guid ChangedByUserId,
    DateTimeOffset OccurredAt);

public sealed record ColumnDeletedMessage(
    Guid BoardId,
    Guid ColumnId,
    Guid ChangedByUserId,
    DateTimeOffset OccurredAt);

public sealed record CardCreatedMessage(
    Guid BoardId,
    CardDto Card,
    Guid ChangedByUserId,
    DateTimeOffset OccurredAt);

public sealed record CardUpdatedMessage(
    Guid BoardId,
    CardDto Card,
    Guid ChangedByUserId,
    DateTimeOffset OccurredAt);

public sealed record CardDeletedMessage(
    Guid BoardId,
    Guid CardId,
    Guid ColumnId,
    Guid ChangedByUserId,
    DateTimeOffset OccurredAt);

public sealed record SprintCreatedMessage(
    Guid BoardId,
    SprintDto Sprint,
    Guid ChangedByUserId,
    DateTimeOffset OccurredAt);

public sealed record SprintStartedMessage(
    Guid BoardId,
    SprintDto Sprint,
    Guid ChangedByUserId,
    DateTimeOffset OccurredAt);

public sealed record SprintUpdatedMessage(
    Guid BoardId,
    SprintDto Sprint,
    Guid ChangedByUserId,
    DateTimeOffset OccurredAt);

public sealed record SprintCompletedMessage(
    Guid BoardId,
    Guid SprintId,
    Guid ChangedByUserId,
    DateTimeOffset OccurredAt);

public sealed record SprintDeletedMessage(
    Guid BoardId,
    Guid SprintId,
    Guid ChangedByUserId,
    DateTimeOffset OccurredAt);

public sealed record CardSprintChangedMessage(
    Guid BoardId,
    CardDto Card,
    Guid? OldSprintId,
    Guid? NewSprintId,
    Guid ChangedByUserId,
    DateTimeOffset OccurredAt);

public sealed record CardDependencyAddedMessage(
    Guid BoardId,
    Guid DependencyId,
    Guid BlockedCardId,
    Guid BlockingCardId,
    Guid ChangedByUserId,
    DateTimeOffset OccurredAt);

public sealed record CardDependencyRemovedMessage(
    Guid BoardId,
    Guid DependencyId,
    Guid BlockedCardId,
    Guid BlockingCardId,
    Guid ChangedByUserId,
    DateTimeOffset OccurredAt);
