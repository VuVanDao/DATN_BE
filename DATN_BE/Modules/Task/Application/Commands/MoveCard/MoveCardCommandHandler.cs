using DATN_BE.Modules.Security.Domain.Enum;
using DATN_BE.Shared.Application.Events;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;

namespace DATN_BE.Modules.Task.Application.Commands.MoveCard
{
    public class MoveCardCommandHandler : IRequestHandler<MoveCardCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly IHubContext<BoardHub> _hubContext;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<MoveCardCommandHandler> _logger;
        private readonly IMediator? _mediator;

        public MoveCardCommandHandler(IApplicationDbContext context, IProjectAuthorizationService authorizationService,
            IHubContext<BoardHub> hubContext, ICurrentUserService currentUser, ILogger<MoveCardCommandHandler> logger,
            IMediator? mediator = null)
        {
            _context = context;
            _authorizationService = authorizationService;
            _hubContext = hubContext;
            _currentUser = currentUser;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<bool> Handle(MoveCardCommand request, CancellationToken cancellationToken)
        {
            var targetCard = await _context.Cards
                .Include(c => c.Column).ThenInclude(c => c.Board)
                .Include(c => c.BlockedByDependencies)
                    .ThenInclude(d => d.BlockingCard)
                        .ThenInclude(bc => bc.Column)
                .FirstOrDefaultAsync(c => c.Id == request.CardId, cancellationToken)
                ?? throw new NotFoundException("Card", request.CardId);
            await _authorizationService.EnsureCanModifyProjectAsync(targetCard.Column.Board.ProjectId, cancellationToken);

            var targetColumn = await _context.Columns.Include(c => c.Board)
                .FirstOrDefaultAsync(c => c.Id == request.TargetColumnId, cancellationToken)
                ?? throw new NotFoundException("Column", request.TargetColumnId);
            if (targetCard.Column.BoardId != targetColumn.BoardId)
            {
                throw new BadRequestException("Không thể di chuyển thẻ giữa các Bảng khác nhau.");
            }

            if (targetColumn.IsDoneColumn)
            {
                var unfinishedBlockers = targetCard.BlockedByDependencies
                    .Where(d => d.BlockingCard != null
                             && !d.BlockingCard.IsDeleted
                             && !((d.BlockingCard.Column != null && d.BlockingCard.Column.IsDoneColumn) || d.BlockingCard.IsCompleted))
                    .Select(d => d.BlockingCard.Title)
                    .ToList();

                if (unfinishedBlockers.Count > 0)
                {
                    var names = string.Join(", ", unfinishedBlockers.Take(3).Select(t => $"\"{t}\""));
                    throw new BadRequestException($"Không thể chuyển thẻ vào cột Hoàn thành vì đang bị chặn bởi công việc chưa xong: {names}.");
                }
            }
            // id của cột gốc của card đang được kéo thả
            var oldColumnId = targetCard.ColumnId;
            var oldColumnTitle = targetCard.Column?.Title;
            var isOldColumnDone = targetCard.Column?.IsDoneColumn ?? false;

            if (oldColumnId == request.TargetColumnId)
            {
                // TH1: KÉO THẢ TRONG CÙNG 1 CỘT
                // tìm tất cả các card trong cột gốc (oldColumnId) ngoại trừ card đang được kéo thả (targetCard)
                var cardsInColumn = await _context.Cards
                    .Where(c => c.ColumnId == oldColumnId && c.Id != targetCard.Id)
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
                var cardsInOldColumn = await _context.Cards
                    .Where(c => c.ColumnId == oldColumnId && c.Id != targetCard.Id)
                    .OrderBy(c => c.Position)
                    .ToListAsync(cancellationToken);
                // sắp xếp lại position của các card trong CỘT gốc
                for (int i = 0; i < cardsInOldColumn.Count; i++)
                {
                    cardsInOldColumn[i].Position = i;
                }

                // Lấy danh sách các card của CỘT đích (chèn targetCard vào)
                var targetCards = await _context.Cards
                    .Where(c => c.ColumnId == request.TargetColumnId && c.Id != targetCard.Id)
                    .OrderBy(c => c.Position)
                    .ToListAsync(cancellationToken);

                // Đổi colId của targetCard sang cột mới
                targetCard.ColumnId = request.TargetColumnId;

                // Cập nhật trạng thái hoàn thành 2 chiều:
                // Kéo vào cột Done -> tự động hoàn thành; Kéo ra khỏi cột Done -> huỷ hoàn thành
                if (targetColumn.IsDoneColumn)
                {
                    targetCard.IsCompleted = true;
                }
                else if (isOldColumnDone)
                {
                    targetCard.IsCompleted = false;
                }

                targetCards.Insert(Math.Clamp(request.NewPosition, 0, targetCards.Count), targetCard);
                // sắp xếp lại position của các card trong CỘT đích, bao gồm cả targetCard
                for (int i = 0; i < targetCards.Count; i++)
                {
                    targetCards[i].Position = i;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (_mediator != null && _currentUser.UserId.HasValue && oldColumnId != request.TargetColumnId)
            {
                await _mediator.Publish(new EntityActivityLoggedEvent(
                    _currentUser.UserId.Value,
                    ActivityEntityType.Card,
                    targetCard.Id,
                    ActivityActionType.MovedToColumn,
                    new
                    {
                        cardTitle = targetCard.Title,
                        fromColumnId = oldColumnId,
                        fromColumnTitle = oldColumnTitle,
                        toColumnId = targetColumn.Id,
                        toColumnTitle = targetColumn.Title,
                        newPosition = targetCard.Position
                    }), cancellationToken);
            }
            try
            {
                await _hubContext.Clients.Group(BoardHub.GroupName(targetColumn.BoardId))
                    .SendAsync(BoardHub.CardMoved,
                        new CardMovedMessage(
                            targetColumn.BoardId,
                            targetCard.Id,
                            oldColumnId,
                            targetCard.ColumnId,
                            targetCard.Position,
                            targetCard.IsCompleted,
                            _currentUser.UserId,
                            DateTimeOffset.UtcNow),
                        CancellationToken.None);
                _logger.LogInformation("Đã phát sự kiện move card cho board {BoardId}.", targetColumn.BoardId);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Không thể phát sự kiện move card cho board {BoardId}.", targetColumn.BoardId);
            }
            return true;
        }
    }
}
