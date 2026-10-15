using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain;
using DATN_BE.Shared.Domain.Constants;
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
                ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này.");

            var board = await _context.Boards
                .FirstOrDefaultAsync(item => item.Id == request.BoardId, cancellationToken)
                ?? throw new NotFoundException("Board", request.BoardId);

            await _authorizationService.EnsureCanModifyProjectAsync(board.ProjectId, cancellationToken);

            var isProjectLeader = await _context.ProjectMembers
                .AnyAsync(member =>
                    member.ProjectId == board.ProjectId &&
                    member.UserId == currentUserId &&
                    member.Role.Name == RoleConstants.ProjectLeader,
                    cancellationToken);

            if (!isProjectLeader)
            {
                throw new ForbiddenException("Chỉ Trưởng dự án mới được xoá Bảng.");
            }

            var columns = await _context.Columns
                .IgnoreQueryFilters()
                .Where(column => column.BoardId == board.Id)
                .ToListAsync(cancellationToken);
            var columnIds = columns.Select(column => column.Id).ToList();
            
            var cards = columnIds.Count == 0
                ? new List<Card>()
                : await _context.Cards
                    .IgnoreQueryFilters()
                    .Where(card => columnIds.Contains(card.ColumnId))
                    .ToListAsync(cancellationToken);

            var deletedAt = DateTimeOffset.UtcNow;
            MarkAsDeleted(board, deletedAt);
            
            foreach (var column in columns)
            {
                MarkAsDeleted(column, deletedAt);
            }

            foreach (var card in cards)
            {
                MarkAsDeleted(card, deletedAt);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static void MarkAsDeleted(ISoftDelete entity, DateTimeOffset deletedAt)
        {
            if (entity.IsDeleted)
            {
                return;
            }

            entity.IsDeleted = true;
            entity.DeletedAt = deletedAt;
        }
    }
}
