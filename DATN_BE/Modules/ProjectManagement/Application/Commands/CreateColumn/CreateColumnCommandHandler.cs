using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.CreateColumn
{
    public class CreateColumnCommandHandler : IRequestHandler<CreateColumnCommand, ColumnDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;
        public CreateColumnCommandHandler(IApplicationDbContext context, IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }
        public async Task<ColumnDto> Handle(CreateColumnCommand request, CancellationToken cancellationToken)
        {
            var targetBoard = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy board");
            await _authorizationService.EnsureCanModifyProjectAsync(targetBoard.ProjectId, cancellationToken);
            // Tự động tính vị trí Position tiếp theo (max + 1)
            var maxPosotion = await _context.Columns.Where(c => c.BoardId == request.BoardId).Select(c => (int?)c.Position).MaxAsync(cancellationToken) ?? -1;
            var newColumn = new Column
            {
                Title = request.Title,
                BoardId = request.BoardId,
                IsDoneColumn = request.IsDoneColumn,
                Position = maxPosotion +1
            };

            _context.Columns.Add(newColumn);
            await _context.SaveChangesAsync();

            return new ColumnDto
            {
                Id = newColumn.Id,
                Title = newColumn.Title,
                Position = newColumn.Position,
                IsDoneColumn = newColumn.IsDoneColumn,
                BoardId = newColumn.BoardId
            };
        }
    }
}
