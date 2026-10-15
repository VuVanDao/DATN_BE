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
                throw new ForbiddenException("Chỉ Trưởng dự án mới được cập nhật Bảng.");
            }

            var slug = SlugHelper.GenerateSlug(request.Slug);
            if (string.IsNullOrWhiteSpace(slug))
            {
                throw new BadRequestException("Slug của Bảng không chứa ký tự hợp lệ.");
            }

            var slugExists = await _context.Boards
                .AnyAsync(item =>
                    item.ProjectId == board.ProjectId &&
                    item.Slug == slug &&
                    item.Id != board.Id,
                    cancellationToken);

            if (slugExists)
            {
                throw new BadRequestException("Slug này đã được sử dụng trong Project.");
            }

            board.Title = request.Title.Trim();
            board.Slug = slug;
            board.Type = request.Type;

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
