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
    Guid BoardId, Guid CardId, Guid SourceColumnId, Guid TargetColumnId,
    int NewPosition, Guid? ChangedByUserId, DateTimeOffset OccurredAt);
