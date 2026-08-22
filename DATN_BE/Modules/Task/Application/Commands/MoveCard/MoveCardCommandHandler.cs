using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Task.Application.Commands.MoveCard
{
    public class MoveCardCommandHandler : IRequestHandler<MoveCardCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public MoveCardCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(MoveCardCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm Card và Cột đích
            var targetCard = await _context.Cards.FirstOrDefaultAsync(c => c.Id == request.CardId, cancellationToken)
                ?? throw new NotFoundException("Card", request.CardId);

            var targetColumn = await _context.Columns.FirstOrDefaultAsync(c => c.Id == request.TargetColumnId, cancellationToken)
                ?? throw new NotFoundException("Column", request.TargetColumnId);
            // id của cột gốc của card đang được kéo thả
            var sourceColumnId = targetCard.ColumnId;

            if (sourceColumnId == request.TargetColumnId)
            {
                // TH1: KÉO THẢ TRONG CÙNG 1 CỘT
                // tìm tất cả các card trong cột gốc (sourceColumnId) ngoại trừ card đang được kéo thả (targetCard)
                var cardsInColumn = await _context.Cards
                    .Where(c => c.ColumnId == sourceColumnId && c.Id != targetCard.Id)
                    .OrderBy(c => c.Position)
                    .ToListAsync(cancellationToken);

                // Chèn card vào vị trí mới
                cardsInColumn.Insert(Math.Clamp(request.NewPosition, 0, cardsInColumn.Count), targetCard);

                // Cập nhật lại Position cho tất cả thẻ trong cột
                for (int i = 0; i < cardsInColumn.Count; i++)
                {
                    cardsInColumn[i].Position = i;
                }
            }
            else
            {
                // TH2: KÉO THẢ SANG CỘT KHÁC
                // Tìm danh sách các card của CỘT gốc (loại bỏ targetCard)
                var sourceCards = await _context.Cards
                    .Where(c => c.ColumnId == sourceColumnId && c.Id != targetCard.Id)
                    .OrderBy(c => c.Position)
                    .ToListAsync(cancellationToken);
                // sắp xếp lại position của các card trong CỘT gốc
                for (int i = 0; i < sourceCards.Count; i++)
                {
                    sourceCards[i].Position = i;
                }

                // Lấy danh sách các card của CỘT đích (chèn targetCard vào)
                var targetCards = await _context.Cards
                    .Where(c => c.ColumnId == request.TargetColumnId && c.Id != targetCard.Id)
                    .OrderBy(c => c.Position)
                    .ToListAsync(cancellationToken);

                // đổi colId của targetCard sang cột mới
                targetCard.ColumnId = request.TargetColumnId;

                // Nếu kéo sang Cột Done -> Tự động đánh dấu hoàn thành 100%
                if (targetColumn.IsDoneColumn)
                {
                    targetCard.ProgressPercentage = 100;
                }

                targetCards.Insert(Math.Clamp(request.NewPosition, 0, targetCards.Count), targetCard);
                // sắp xếp lại position của các card trong CỘT đích, bao gồm cả targetCard
                for (int i = 0; i < targetCards.Count; i++)
                {
                    targetCards[i].Position = i;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
