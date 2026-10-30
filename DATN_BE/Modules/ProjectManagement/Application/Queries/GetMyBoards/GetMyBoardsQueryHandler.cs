using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetMyBoards;

public class GetMyBoardsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    : IRequestHandler<GetMyBoardsQuery, MyBoardsDto>
{
    public async Task<MyBoardsDto> Handle(GetMyBoardsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenException("Bạn cần đăng nhập để xem danh sách Bảng.");

        // Recheck current view access: old preferences may survive membership removal.
        var boards = context.UserBoardPreferences.AsNoTracking()
            .Where(preference => preference.UserId == userId &&
                context.Boards.Any(board => board.Id == preference.BoardId &&
                    context.Projects.Any(project => project.Id == board.ProjectId &&
                        context.Workspaces.Any(workspace => workspace.Id == project.WorkspaceId &&
                            (workspace.OwnerId == userId ||
                             project.ProjectMembers.Any(member => member.UserId == userId))))))
            .Select(preference => new MyBoardItemDto
            {
                BoardId = preference.BoardId,
                Title = preference.Board.Title,
                BoardSlug = preference.Board.Slug,
                ProjectId = preference.Board.ProjectId,
                IsPinned = preference.IsPinned,
                LastViewedAt = preference.LastViewedAt
            });

        var recent = await boards.OrderByDescending(board => board.LastViewedAt)
            .ThenBy(board => board.BoardId).Take(3).ToListAsync(cancellationToken);
        var pinned = await boards.Where(board => board.IsPinned)
            .OrderByDescending(board => board.LastViewedAt)
            .ThenBy(board => board.BoardId).Take(3).ToListAsync(cancellationToken);

        return new MyBoardsDto { RecentBoards = recent, PinnedBoards = pinned };
    }
}
