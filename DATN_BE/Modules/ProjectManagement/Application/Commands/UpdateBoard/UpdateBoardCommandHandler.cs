using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using DATN_BE.Shared.Utils;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateBoard
{
    public class UpdateBoardCommandHandler : IRequestHandler<UpdateBoardCommand, BoardDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectAuthorizationService _authorizationService;

        public UpdateBoardCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _authorizationService = authorizationService;
        }

        public async Task<BoardDto> Handle(UpdateBoardCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này.");

            var targetBoard = await _context.Boards
                .FirstOrDefaultAsync(item => item.Id == request.BoardId, cancellationToken)
                ?? throw new NotFoundException("Board", request.BoardId);

            await _authorizationService.EnsureProjectLeaderAsync(targetBoard.ProjectId, cancellationToken);

            var normalizedTitle = request.Title.Trim();

            if (!string.Equals(targetBoard.Title, normalizedTitle, StringComparison.OrdinalIgnoreCase))
            {
                var baseSlug = SlugHelper.GenerateSlug(normalizedTitle);
                if (string.IsNullOrWhiteSpace(baseSlug))
                {
                    baseSlug = "board";
                }
                if (baseSlug.Length > 100)
                {
                    baseSlug = baseSlug[..100].TrimEnd('-');
                }

                var existingSlugs = await _context.Boards
                    .Where(b => b.ProjectId == targetBoard.ProjectId
                                && b.Id != targetBoard.Id
                                && (b.Slug == baseSlug || b.Slug.StartsWith(baseSlug + "-")))
                    .Select(item => item.Slug)
                    .ToHashSetAsync(cancellationToken);

                var slug = baseSlug;
                while (existingSlugs.Contains(slug))
                {
                    var shortCode = SlugHelper.GenerateShortCode(6);
                    slug = $"{baseSlug}-{shortCode}";
                }

                targetBoard.Slug = slug;
            }

            targetBoard.Title = normalizedTitle;
            targetBoard.Type = request.Type;

            await _context.SaveChangesAsync(cancellationToken);

            return new BoardDto
            {
                Id = targetBoard.Id,
                ProjectId = targetBoard.ProjectId,
                Title = targetBoard.Title,
                Slug = targetBoard.Slug,
                Type = targetBoard.Type.ToString(),
                CreatedAt = targetBoard.CreatedAt
            };
        }
    }
}
