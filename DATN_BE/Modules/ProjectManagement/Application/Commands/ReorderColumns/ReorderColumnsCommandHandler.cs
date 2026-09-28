using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.ReorderColumns
{
    public class ReorderColumnsCommandHandler : IRequestHandler<ReorderColumnsCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;

        public ReorderColumnsCommandHandler(IApplicationDbContext context, IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }

        public async Task<bool> Handle(ReorderColumnsCommand request, CancellationToken cancellationToken)
        {
            if (request.OrderedColumnIds == null || !request.OrderedColumnIds.Any())
            {
                throw new BadRequestException("Danh sách column cần sắp xếp không được để trống.");
            }
            var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy board.");
            await _authorizationService.EnsureCanModifyProjectAsync(board.ProjectId, cancellationToken);
            // tìm kiếm tất cả các cột với BoardId tương ứng
            var columns = await _context.Columns
                .Where(c => c.BoardId == request.BoardId)
                .ToListAsync(cancellationToken);

            if (!columns.Any())
            {
                throw new NotFoundException("Không tìm thấy cột nào trong Bảng này.");
            }

            var requestedColumnIds = request.OrderedColumnIds.ToHashSet();
            var boardColumnIds = columns.Select(column => column.Id).ToHashSet();
            if (requestedColumnIds.Count != request.OrderedColumnIds.Count ||
                !requestedColumnIds.SetEquals(boardColumnIds))
            {
                throw new BadRequestException("Danh sách column phải chứa đúng một lần tất cả column thuộc Board.");
            }

            for (int i = 0; i < request.OrderedColumnIds.Count; i++)
            {
                var col = columns.First(column => column.Id == request.OrderedColumnIds[i]);
                col.Position = i;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
