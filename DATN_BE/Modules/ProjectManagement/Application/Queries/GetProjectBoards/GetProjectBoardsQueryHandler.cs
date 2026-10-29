using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectBoards
{
    public class GetProjectBoardsQueryHandler : IRequestHandler<GetProjectBoardsQuery, List<BoardDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetProjectBoardsQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<BoardDto>> Handle(
            GetProjectBoardsQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để xem danh sách Bảng.");

            var projectExists = await _context.Projects
                .AsNoTracking()
                .AnyAsync(project => project.Id == request.ProjectId, cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundException("Project", request.ProjectId);
            }

            var hasAccess = await _context.Projects
                .AsNoTracking()
                .AnyAsync(p => p.Id == request.ProjectId &&
                    (p.Workspace.OwnerId == currentUserId ||
                     p.ProjectMembers.Any(pm => pm.UserId == currentUserId)),
                    cancellationToken);

            if (!hasAccess)
            {
                throw new ForbiddenException("Bạn chưa tham gia Project này nên không thể xem danh sách Bảng.");
            }

            return await _context.Boards
                .AsNoTracking()
                .Where(board => board.ProjectId == request.ProjectId)
                .OrderByDescending(board => board.UserBoardPreferences
                    .Where(p => p.UserId == currentUserId)
                    .Select(p => p.IsPinned)
                    .FirstOrDefault())
                .ThenBy(board => board.CreatedAt)
                .Select(board => new BoardDto
                {
                    Id = board.Id,
                    ProjectId = board.ProjectId,
                    Title = board.Title,
                    Slug = board.Slug,
                    Type = board.Type.ToString(),
                    CreatedAt = board.CreatedAt,
                    IsPinned = board.UserBoardPreferences
                        .Where(p => p.UserId == currentUserId)
                        .Select(p => p.IsPinned)
                        .FirstOrDefault(),
                    LastViewedAt = board.UserBoardPreferences
                        .Where(p => p.UserId == currentUserId)
                        .Select(p => (DateTimeOffset?)p.LastViewedAt)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);
        }
    }
}
