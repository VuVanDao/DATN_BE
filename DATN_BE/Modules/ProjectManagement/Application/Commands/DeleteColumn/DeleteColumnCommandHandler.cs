using DATN_BE.Modules.Security.Domain.Enum;
using DATN_BE.Shared.Application.Events;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteColumn;

public class DeleteColumnCommandHandler(
    IApplicationDbContext context,
    IProjectAuthorizationService authorizationService,
    IHubContext<BoardHub>? hubContext = null,
    ICurrentUserService? currentUserService = null,
    ILogger<DeleteColumnCommandHandler>? logger = null,
    IMediator? mediator = null) : IRequestHandler<DeleteColumnCommand, bool>
{
    public async Task<bool> Handle(DeleteColumnCommand request, CancellationToken cancellationToken)
    {
        var board = await context.Boards.AsNoTracking()
            .Where(b =>
                context.Projects.Any(p => p.Id == b.ProjectId &&
                    context.Workspaces.Any(w => w.Id == p.WorkspaceId)))
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, cancellationToken)
            ?? throw new NotFoundException("Board", request.BoardId);

        await authorizationService.EnsureCanModifyProjectAsync(board.ProjectId, cancellationToken);

        var column = await context.Columns
            .FirstOrDefaultAsync(c => c.Id == request.ColumnId && c.BoardId == board.Id, cancellationToken)
            ?? throw new NotFoundException("Column", request.ColumnId);

        var cards = await context.Cards.IgnoreQueryFilters()
            .Where(c => c.ColumnId == column.Id && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        var deletedAt = DateTimeOffset.UtcNow;
        var columnTitle = column.Title;
        column.IsDeleted = true;
        column.DeletedAt = deletedAt;
        foreach (var card in cards)
        {
            card.IsDeleted = true;
            card.DeletedAt = deletedAt;
        }

        await context.SaveChangesAsync(cancellationToken);

        if (mediator != null && currentUserService?.UserId is Guid actorId)
        {
            await mediator.Publish(new EntityActivityLoggedEvent(
                actorId,
                ActivityEntityType.Column,
                column.Id,
                ActivityActionType.Deleted,
                new
                {
                    columnTitle,
                    boardId = column.BoardId,
                    cardsCount = cards.Count
                }), cancellationToken);
        }

        if (hubContext != null)
        {
            try
            {
                await hubContext.Clients
                    .Group(BoardHub.GroupName(board.Id))
                    .SendAsync(
                        BoardHub.ColumnDeleted,
                        new ColumnDeletedMessage(
                            board.Id,
                            column.Id,
                            currentUserService?.UserId ?? Guid.Empty,
                            DateTimeOffset.UtcNow),
                        CancellationToken.None);
            }
            catch (Exception exception)
            {
                logger?.LogWarning(exception, "Không thể phát sự kiện ColumnDeleted cho board {BoardId}.", board.Id);
            }
        }

        return true;
    }
}
