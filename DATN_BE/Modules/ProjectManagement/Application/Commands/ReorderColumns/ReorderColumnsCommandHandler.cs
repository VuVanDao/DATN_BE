using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.ReorderColumns
{
    public class ReorderColumnsCommandHandler : IRequestHandler<ReorderColumnsCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        public ReorderColumnsCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<bool> Handle(ReorderColumnsCommand request, CancellationToken cancellationToken)
        {
            // tìm tất cả các column của request.BoardId
            var columns = await _context.Columns.Where(c => c.BoardId == request.BoardId).ToListAsync(cancellationToken);
            if (!columns.Any())
            {
                throw new NotFoundException("Không tìm thấy cột nào trong Bảng này.");
            }
            // Cập nhật lại Position theo đúng thứ tự mảng gửi lên
            // BƯỚC 1: Đổi sang số âm tạm thời để xóa bỏ xung đột Unique Index trong PostgreSQL
            for (int i = 0; i < request.OrderedColumnIds.Count; i++)
            {
                var colId = request.OrderedColumnIds[i];
                var col = columns.FirstOrDefault(c => c.Id == colId);
                if (col != null)
                {
                    col.Position = -(i + 1); // -1, -2, -3...
                }
            }
            await _context.SaveChangesAsync(cancellationToken);

            // BƯỚC 2: Cập nhật lại vị trí chuẩn (0, 1, 2, 3...) theo đúng thứ tự mảng gửi lên
            for (int i = 0; i < request.OrderedColumnIds.Count; i++)
            {
                var colId = request.OrderedColumnIds[i];
                var col = columns.FirstOrDefault(c => c.Id == colId);
                if (col != null)
                {
                    col.Position = i; // 0, 1, 2, 3...
                }
            }
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
