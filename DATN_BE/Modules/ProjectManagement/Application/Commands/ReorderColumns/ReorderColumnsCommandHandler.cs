using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.ReorderColumns
{
    public class ReorderColumnsCommandHandler : IRequestHandler<ReorderColumnsCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly IHubContext<BoardHub> _hubContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ReorderColumnsCommandHandler> _logger;

        public ReorderColumnsCommandHandler(
            IApplicationDbContext context,
            IProjectAuthorizationService authorizationService,
            IHubContext<BoardHub> hubContext,
            ICurrentUserService currentUserService,
            ILogger<ReorderColumnsCommandHandler> logger)
        {
            _context = context;
            _authorizationService = authorizationService;
            _hubContext = hubContext;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<bool> Handle(ReorderColumnsCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new UnauthorizedException("Bạn cần đăng nhập để thực hiện hành động này");
                
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

            try
            {
                await _hubContext.Clients
                    .Group(BoardHub.GroupName(request.BoardId))
                    .SendAsync(
                        BoardHub.ColumnsReordered,// tên sk, client sẽ nghe tên sk này để xử lý
                        new ColumnsReorderedMessage(
                            request.BoardId,
                            request.OrderedColumnIds,
                            currentUserId,
                            DateTimeOffset.UtcNow),
                        CancellationToken.None);
            }
            catch (Exception exception)
            {
                // Database đã lưu thành công; lỗi phát realtime không nên biến kết quả reorder thành 500.
                _logger.LogWarning(
                    exception,
                    "Không thể phát sự kiện reorder columns cho board {BoardId}.",
                    request.BoardId);
            }

            return true;
        }
    }
}
