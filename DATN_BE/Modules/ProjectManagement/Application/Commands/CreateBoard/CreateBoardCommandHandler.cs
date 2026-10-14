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
                ?? throw new ForbiddenException("Bạn cần đăng nhập để thực hiện hành động này.");
                
            var projectExists = await _context.Projects
                .AsNoTracking()
                .AnyAsync(project => project.Id == request.ProjectId, cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundException("Project", request.ProjectId);
            }

            // This check verifies that the creator is a member of the project.
            await _authorizationService.EnsureCanModifyProjectAsync(request.ProjectId, cancellationToken);

            var isProjectLeader = await _context.ProjectMembers
                .AnyAsync(pm =>
                    pm.ProjectId == request.ProjectId &&
                    pm.UserId == currentUserId &&
                    pm.Role.Name == RoleConstants.ProjectLeader,
                    cancellationToken);

            if (!isProjectLeader)
            {
                throw new ForbiddenException("Chỉ Trưởng dự án mới được tạo Bảng.");
            }

            var slug = SlugHelper.GenerateSlug(request.Slug);
            if (string.IsNullOrWhiteSpace(slug))
            {
                throw new BadRequestException("Slug của Bảng không chứa ký tự hợp lệ.");
            }

            var slugExists = await _context.Boards
                .AnyAsync(board => board.ProjectId == request.ProjectId && board.Slug == slug, cancellationToken);

            if (slugExists)
            {
                throw new BadRequestException("Slug này đã được sử dụng trong Project.");
            }

            var board = new Board
            {
                ProjectId = request.ProjectId,
                Title = request.Title.Trim(),
                Slug = slug,
                Type = request.Type
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
