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
            if (request.OrderedColumnIds == null || !request.OrderedColumnIds.Any())
            {
                return false;
            }
            // Xây dựng câu lệnh UPDATE CASE WHEN động
            var caseStatements = new List<string>();
            for (int i = 0; i < request.OrderedColumnIds.Count; i++)
            {
                caseStatements.Add($"WHEN '{request.OrderedColumnIds[i]}' THEN {i}");
            }
            var inClause = string.Join(", ", request.OrderedColumnIds.Select(id => $"'{id}'"));
            var sql = $@"
            -- Bước 1: Dời toàn bộ cột của Board sang số âm để xóa sạch xung đột Unique
                UPDATE ""Columns""
                SET ""Position"" = - (""Position"" + 1000)
                WHERE ""BoardId"" = '{request.BoardId}';
            -- Bước 2: Cập nhật vị trí mới theo đúng thứ tự mảng
                UPDATE ""Columns""
                SET ""Position"" = CASE ""Id""
                    {string.Join(" ", caseStatements)}
                END
                WHERE ""BoardId"" = '{request.BoardId}' AND ""Id"" IN ({inClause});
        ";

            // Thực thi ĐÚNG 1 LẦN DUY NHẤT xuống PostgreSQL
            await ((DbContext)_context).Database.ExecuteSqlRawAsync(sql, cancellationToken);
            return true;
        }
    }
}
