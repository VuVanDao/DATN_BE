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
                return false;
            }
            // tìm kiếm tất cả các cột trong bảng với BoardId tương ứng
            var board = await _context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy board.");
            await _authorizationService.EnsureCanModifyProjectAsync(board.ProjectId, cancellationToken);
            var columns = await _context.Columns
                .Where(c => c.BoardId == request.BoardId)
                .ToListAsync(cancellationToken);

            if (!columns.Any())
            {
                throw new NotFoundException("Không tìm thấy cột nào trong Bảng này.");
            }
            // sau khi lấy được danh sách col trong database, ta sẽ duyệt qua danh sách OrderedColumnIds và cập nhật lại position của từng cột theo thứ tự mới
            for (int i = 0; i < request.OrderedColumnIds.Count; i++)
            {
                var col = columns.FirstOrDefault(c => c.Id == request.OrderedColumnIds[i]);
                if (col != null)
                {
                    col.Position = i;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
