using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using DATN_BE.Shared.Utils;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.CreateBoard
{
    public class CreateBoardCommandHandler : IRequestHandler<CreateBoardCommand, BoardDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectAuthorizationService _authorizationService;

        public CreateBoardCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _authorizationService = authorizationService;
        }

        public async Task<BoardDto> Handle(CreateBoardCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new UnauthorizedException("Bạn cần đăng nhập để thực hiện hành động này.");
                
            var projectExists = await _context.Projects
                .AsNoTracking()
                .AnyAsync(project => project.Id == request.ProjectId, cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundException("Project", request.ProjectId);
            }

            await _authorizationService.EnsureProjectLeaderAsync(request.ProjectId, cancellationToken);

            // 1. Tự động sinh Slug theo Title và xử lý chống trùng Slug trong Project
            var baseSlug = SlugHelper.GenerateSlug(request.Title);
            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                baseSlug = "board";
            }
            if (baseSlug.Length > 100)
            {
                baseSlug = baseSlug[..100].TrimEnd('-');
            }

            var existingSlugs = await _context.Boards
                .Where(board => board.ProjectId == request.ProjectId && (board.Slug == baseSlug || board.Slug.StartsWith(baseSlug + "-")))
                .Select(board => board.Slug)
                .ToHashSetAsync(cancellationToken);

            var slug = baseSlug;
            while (existingSlugs.Contains(slug))
            {
                var shortCode = SlugHelper.GenerateShortCode(6);
                slug = $"{baseSlug}-{shortCode}";
            }

            var board = new Board
            {
                ProjectId = request.ProjectId,
                Title = request.Title.Trim(),
                Slug = slug,
                Type = request.Type,
                Columns = new List<Column>
                {
                    new Column { Title = "To Do", Position = 0, IsDoneColumn = false },
                    new Column { Title = "In Progress", Position = 1, IsDoneColumn = false },
                    new Column { Title = "Done", Position = 2, IsDoneColumn = true }
                }
            };

            _context.Boards.Add(board);
            await _context.SaveChangesAsync(cancellationToken);

            return new BoardDto
            {
                Id = board.Id,
                ProjectId = board.ProjectId,
                Title = board.Title,
                Slug = board.Slug,
                Type = board.Type.ToString(),
                CreatedAt = board.CreatedAt
            };
        }
    }
}
