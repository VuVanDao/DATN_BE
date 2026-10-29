using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateBoardPreference;

public class UpdateBoardPreferenceCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    IProjectAuthorizationService authorizationService)
    : IRequestHandler<UpdateBoardPreferenceCommand, BoardPreferenceDto>
{
    public async Task<BoardPreferenceDto> Handle(UpdateBoardPreferenceCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này.");

        // Exclude boards under deleted projects/workspaces as well.
        var board = await context.Boards.AsNoTracking()
            .Where(x => x.Id == request.BoardId && context.Projects.Any(p =>
                p.Id == x.ProjectId && context.Workspaces.Any(w => w.Id == p.WorkspaceId)))
            .Select(x => new { x.Id, x.ProjectId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Board", request.BoardId);

        await authorizationService.EnsureCanModifyProjectAsync(board.ProjectId, cancellationToken);

        var preference = await context.UserBoardPreferences.SingleOrDefaultAsync(
            x => x.UserId == userId && x.BoardId == board.Id, cancellationToken);
        if (preference is null)
        {
            // The existing model initializes LastViewedAt on first creation.
            preference = new UserBoardPreference { UserId = userId, BoardId = board.Id };
            context.UserBoardPreferences.Add(preference);
        }

        if (request.IsPinned.HasValue)
            preference.IsPinned = request.IsPinned.Value;
        if (request.MarkViewed)
            preference.LastViewedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return new BoardPreferenceDto
        {
            BoardId = preference.BoardId,
            IsPinned = preference.IsPinned,
            LastViewedAt = preference.LastViewedAt
        };
    }
}
