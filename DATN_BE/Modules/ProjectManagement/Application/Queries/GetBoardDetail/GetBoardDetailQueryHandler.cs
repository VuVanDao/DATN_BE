using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetBoardDetail
{
    public class GetBoardDetailQueryHandler : IRequestHandler<GetBoardDetailQuery, BoardDetailDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;
        public GetBoardDetailQueryHandler(IApplicationDbContext context, IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }
        public async Task<BoardDetailDto> Handle(GetBoardDetailQuery request, CancellationToken cancellationToken)
        {
            var board = await _context.Boards.AsNoTracking()
                .Include(b => b.Project)
                .Include(b => b.Columns)
                .FirstOrDefaultAsync(b => b.Slug == request.BoardSlug && b.ProjectId == request.ProjectId, cancellationToken)
                ?? throw new NotFoundException("Board", request.BoardSlug);
            await _authorizationService.EnsureCanViewProjectAsync(board.ProjectId, cancellationToken);

            return new BoardDetailDto
            {
                Id = board.Id,
                Title = board.Title,
                Slug = board.Slug,
                Type = board.Type.ToString(),
                ProjectId = board.ProjectId,
                ProjectName = board.Project?.Name ?? string.Empty,
                // Sắp xếp các cột theo thứ tự Position tăng dần từ trái sang phải
                Columns = board.Columns
                .OrderBy(c => c.Position)
                .Select(c => new ColumnDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Position = c.Position,
                    IsDoneColumn = c.IsDoneColumn,
                    BoardId = c.BoardId
                }).ToList()
            };
        }
    }
}
