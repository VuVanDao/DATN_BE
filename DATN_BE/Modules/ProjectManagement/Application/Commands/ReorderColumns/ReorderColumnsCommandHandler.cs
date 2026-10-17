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
            if (request.OrderedColumnIds == null || request.OrderedColumnIds.Count == 0)
            {
                throw new BadRequestException("Danh sách column cần sắp xếp không được để trống.");
            }
            var board = await _context.Boards
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == request.BoardId, cancellationToken)
                ?? throw new NotFoundException("board", request.BoardId);

            await _authorizationService.EnsureCanModifyProjectAsync(board.ProjectId, cancellationToken);
            
            // tìm kiếm tất cả các cột với BoardId tương ứng
            var columns = await _context.Columns
                .Where(c => c.BoardId == request.BoardId)
                .ToListAsync(cancellationToken);

            if (columns.Count == 0)
            {
                throw new NotFoundException("Không tìm thấy cột nào trong Bảng này.");
            }

            var requestedColumnIds = request.OrderedColumnIds.ToHashSet();
            var boardColumnIds = columns.Select(column => column.Id).ToHashSet();

            if (requestedColumnIds.Count != request.OrderedColumnIds.Count ||
                !requestedColumnIds.SetEquals(boardColumnIds))
            {
                throw new BadRequestException("Danh sách column được gửi lên phải chứa đúng một lần tất cả column hiện tại thuộc Board.");
            }

            var currentOrder = columns.OrderBy(c => c.Position).Select(c => c.Id).ToList();
            if (currentOrder.SequenceEqual(request.OrderedColumnIds))
            {
                return true;
            }

            var columnDict = columns.ToDictionary(c => c.Id);

            for (int i = 0; i < request.OrderedColumnIds.Count; i++)
            {
                var columnId = request.OrderedColumnIds[i];
                if (columnDict.TryGetValue(columnId, out var col))
                {
                    col.Position = i;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
