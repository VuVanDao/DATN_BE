using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteColumn;

public class DeleteColumnCommandHandler(
    IApplicationDbContext context,
    IProjectAuthorizationService authorizationService) : IRequestHandler<DeleteColumnCommand, bool>
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
        column.IsDeleted = true;
        column.DeletedAt = deletedAt;
        foreach (var card in cards)
        {
            card.IsDeleted = true;
            card.DeletedAt = deletedAt;
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
