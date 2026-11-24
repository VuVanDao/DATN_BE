using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteBoard
{
    public class DeleteBoardCommandHandler : IRequestHandler<DeleteBoardCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectAuthorizationService _authorizationService;

        public DeleteBoardCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _authorizationService = authorizationService;
        }

        public async Task<bool> Handle(DeleteBoardCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new UnauthorizedException("Bạn cần đăng nhập để thực hiện hành động này.");

            var board = await _context.Boards
                .FirstOrDefaultAsync(item => item.Id == request.BoardId, cancellationToken)
                ?? throw new NotFoundException("Board", request.BoardId);

            await _authorizationService.EnsureProjectLeaderAsync(board.ProjectId, cancellationToken);

            // 1. Lấy danh sách Columns của Board
            var columns = await _context.Columns
                .IgnoreQueryFilters()
                .Where(column => column.BoardId == board.Id)
                .ToListAsync(cancellationToken);

            // 2. Lấy danh sách Cards thuộc Board qua Navigation Property
            var cards = await _context.Cards
                .IgnoreQueryFilters()
                .Where(card => card.Column.BoardId == board.Id)
                .ToListAsync(cancellationToken);

            // 3. Lấy danh sách Sprints thuộc Board (đối với Scrum Board)
            var sprints = await _context.Sprints
                .IgnoreQueryFilters()
                .Where(s => s.BoardId == board.Id)
                .ToListAsync(cancellationToken);

            // 4. Dọn dẹp CardComments và CardDependencies liên quan đến các Cards của Board
            var cardIds = cards.Select(c => c.Id).ToList();
            if (cardIds.Count > 0)
            {
                var cardComments = await _context.CardComments
                    .Where(c => cardIds.Contains(c.CardId))
                    .ToListAsync(cancellationToken);

                if (cardComments.Count > 0)
                {
                    _context.CardComments.RemoveRange(cardComments);
                }

                var dependencies = await _context.CardDependencies
                    .Where(cd => cardIds.Contains(cd.BlockedCardId) || cardIds.Contains(cd.BlockingCardId))
                    .ToListAsync(cancellationToken);

                if (dependencies.Count > 0)
                {
                    _context.CardDependencies.RemoveRange(dependencies);
                }
            }

            // 5. Dọn dẹp UserBoardPreferences của Board
            var preferences = await _context.UserBoardPreferences
                .Where(p => p.BoardId == board.Id)
                .ToListAsync(cancellationToken);

            if (preferences.Count > 0)
            {
                _context.UserBoardPreferences.RemoveRange(preferences);
            }

            // 6. Xoá mềm Sprints, Cards, Columns, Board (ApplicationDbContext tự động gán IsDeleted = true)
            if (sprints.Count > 0)
            {
                _context.Sprints.RemoveRange(sprints);
            }

            if (cards.Count > 0)
            {
                _context.Cards.RemoveRange(cards);
            }

            if (columns.Count > 0)
            {
                _context.Columns.RemoveRange(columns);
            }

            _context.Boards.Remove(board);

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
