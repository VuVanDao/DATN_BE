using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateColumn;

public class UpdateColumnCommandHandler(
    IApplicationDbContext context,
    IProjectAuthorizationService authorizationService) : IRequestHandler<UpdateColumnCommand, ColumnDto>
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

        column.Title = request.Title.Trim();
        column.IsDoneColumn = request.IsDoneColumn;
        await context.SaveChangesAsync(cancellationToken);

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
