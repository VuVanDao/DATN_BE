using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.Security.Domain.Enum;
using DATN_BE.Shared.Application.Events;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateColumn;

public class UpdateColumnCommandHandler(
    IApplicationDbContext context,
    IProjectAuthorizationService authorizationService,
    IHubContext<BoardHub>? hubContext = null,
    ICurrentUserService? currentUserService = null,
    ILogger<UpdateColumnCommandHandler>? logger = null,
    IMediator? mediator = null) : IRequestHandler<UpdateColumnCommand, ColumnDto>
{
    public async Task<ColumnDto> Handle(UpdateColumnCommand request, CancellationToken cancellationToken)
    {
        var board = await context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, cancellationToken)
            ?? throw new NotFoundException("Board", request.BoardId);

        await authorizationService.EnsureCanModifyProjectAsync(board.ProjectId, cancellationToken);

        var column = await context.Columns
            .FirstOrDefaultAsync(c => c.Id == request.ColumnId && c.BoardId == board.Id, cancellationToken)
            ?? throw new NotFoundException("Column", request.ColumnId);

        var oldTitle = column.Title;
        column.Title = request.Title.Trim();
        column.IsDoneColumn = request.IsDoneColumn;
        await context.SaveChangesAsync(cancellationToken);

        if (mediator != null && currentUserService?.UserId is Guid actorId)
        {
            await mediator.Publish(new EntityActivityLoggedEvent(
                actorId,
                ActivityEntityType.Column,
                column.Id,
                ActivityActionType.Updated,
                new
                {
                    oldTitle,
                    newTitle = column.Title,
                    boardId = column.BoardId,
                    isDoneColumn = column.IsDoneColumn
                }), cancellationToken);
        }

        if (hubContext != null)
        {
            try
            {
                await hubContext.Clients
                    .Group(BoardHub.GroupName(board.Id))
                    .SendAsync(
                        BoardHub.ColumnUpdated,
                        new ColumnUpdatedMessage(
                            column.BoardId,
                            column.Id,
                            column.Title,
                            column.Position,
                            column.IsDoneColumn,
                            currentUserService?.UserId ?? Guid.Empty,
                            DateTimeOffset.UtcNow),
                        CancellationToken.None);
            }
            catch (Exception exception)
            {
                logger?.LogWarning(exception, "Không thể phát sự kiện ColumnUpdated cho board {BoardId}.", board.Id);
            }
        }

        return new ColumnDto
        {
            Id = column.Id,
            BoardId = column.BoardId,
            Title = column.Title,
            Position = column.Position,
            IsDoneColumn = column.IsDoneColumn
        };
    }
}
