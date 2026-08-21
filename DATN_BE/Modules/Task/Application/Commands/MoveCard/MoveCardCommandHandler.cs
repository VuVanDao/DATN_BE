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
            // tim card theo id
            var targetCard = await _context.Cards.FirstOrDefaultAsync(c => c.Id == request.CardId, cancellationToken)
                    ?? throw new NotFoundException($"Không tìm thấy card với id {request.CardId}");
            // tim column theo id
            var targetColumn = await _context.Columns.FirstOrDefaultAsync(c => c.Id == request.TargetColumnId, cancellationToken)
                    ?? throw new NotFoundException($"Không tìm thấy column với id {request.TargetColumnId}");
            var sourceColumnId = targetCard.ColumnId;// card hiện tại đang ở column nào
            var oldPosition = targetCard.Position;// vị trí hiện tại của card trong column

            // BƯỚC 1: Dời tạm thời toàn bộ Card liên quan sang số âm để tránh xung đột Unique Index
            // VD: A (0), B (1), C (2), D (3).
            var shiftNegativeSql = $@"
                UPDATE ""Cards""
                SET ""Position"" = - (""Position"" + 1000)
                WHERE ""ColumnId"" IN ('{sourceColumnId}', '{request.TargetColumnId}');";
            await ((DbContext)_context).Database.ExecuteSqlRawAsync(shiftNegativeSql, cancellationToken);
            // sau đoạn trên sẽ là A (-1000), B (-1001), C (-1002), D (-1003)

            if (sourceColumnId == request.TargetColumnId)
            {
                // th1: di chuyển trong cùng 1 cột
                var cardsInColumn = await _context.Cards
                    .Where(c => c.ColumnId == sourceColumnId && c.Id != targetCard.Id)
                    .OrderBy(c => -c.Position - 1000) // Đọc lại theo thứ tự cũ
                    .ToListAsync(cancellationToken);
                // sau đoạn trên sẽ là A (0), B (1), C (2), D (3), dù thưc chất trong db là A (-1000), B (-1001), C (-1002), D (-1003)

                // Math.Clamp để đảm bảo newPosition nằm trong khoảng từ 0 đến cardsInColumn.Count
                cardsInColumn.Insert(Math.Clamp(request.NewPosition, 0, cardsInColumn.Count), targetCard);

                for (int i = 0; i < cardsInColumn.Count; i++)
                {
                    cardsInColumn[i].Position = i;
                }
            }
            else
            {
                // th2: di chuyển sang cột khác
                // Sắp xếp lại cột NGUỒN (loại bỏ card cũ)
                var sourceCards = await _context.Cards
                   .Where(c => c.ColumnId == sourceColumnId && c.Id != targetCard.Id)
                   .OrderBy(c => -c.Position - 1000)
                   .ToListAsync(cancellationToken);
                // sau đoạn trên sẽ là A (-1000-0), B (-1001-1), C (-1002-2) [position-index] dù thưc chất trong db là A (-1000), B (-1001), C (-1002), D (-1003)
                for (int i = 0; i < sourceCards.Count; i++)
                {
                    sourceCards[i].Position = i;
                }
                // sau đoạn trên sẽ là A (0-0), B (1-1), C (2-2) [position-index]

                // Chèn card vào CỘT ĐÍCH
                // tìm tất cả card trong cột đích (request.TargetColumnId) (ngoại trừ card đang di chuyển) và sắp xếp lại
                var targetListCard = await _context.Cards
                    .Where(c => c.ColumnId == request.TargetColumnId && c.Id != targetCard.Id)
                    .OrderBy(c => -c.Position - 1000)
                    .ToListAsync(cancellationToken);
                // sau đoạn trên ta được M (-1000-0), N (-1001-1), O (-1002-2) [position-index] dù thưc chất trong db là M (-1000), N (-1001), O (-1002)

                // đổi columnId của targetCard sang cột đích
                targetCard.ColumnId = request.TargetColumnId;
                // Nếu kéo vào Cột Done -> Tự động đánh dấu hoàn thành 100%
                if (targetColumn.IsDoneColumn)
                {
                    targetCard.ProgressPercentage = 100;
                }
                // chèn targetCard vào vị trí mới ở cột mới
                targetListCard.Insert(Math.Clamp(request.NewPosition, 0, targetListCard.Count), targetCard);
                // ví dụ request.NewPosition = 0 thì targetCard sẽ chèn vào đầu danh sách targetListCard, các card còn lại sẽ được đẩy xuống 
                // sau đoạn trên ta có targetListCard là [targetCard, M, N, O] với position-index là [-1003-0, -1000-1, -1002-2, -1003-3] dù thưc chất trong db là M (-1000), N (-1001), O (-1002)

                for (int i = 0; i < targetListCard.Count; i++)
                {
                    targetListCard[i].Position = i;
                }
                // sau đoạn trên ta có targetListCard là [targetCard, M, N, O] với position-index là [0-0, 1-1, 2-2, 3-3]
            }
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
