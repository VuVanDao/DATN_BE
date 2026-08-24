using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Application.Commands.UpdateProfile
{
    public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserProfileDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public UpdateProfileCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        public async Task<UserProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
        {
            // lấy userId trong token người dùng gửi lên
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("Người dùng chưa đăng nhập, yêu cầu đăng nhập");
            // lấy user tương ứng với UserId
            var user = await _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken) ?? throw new NotFoundException("Không tìm thấy user tương ứng");
            // kiểm tra trùng username nếu người dùng đổi username mới
            var newUsername = request.Username.Trim();
            if (!string.Equals(user.Username, newUsername, StringComparison.OrdinalIgnoreCase))
            {
                var checkUsernameexisted = await _context.Users.AnyAsync(u => u.Username.ToLower() == newUsername.ToLower() && u.Id != user.Id,cancellationToken);
                if (checkUsernameexisted)
                {
                    throw new BadRequestException($"Username '{newUsername}' đã được sử dụng bởi tài khoản khác.");
                }
                user.Username = newUsername;
            }
            user.Bio = request.Bio?.Trim();

            await _context.SaveChangesAsync(cancellationToken);

            return new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                AvatarUrl = user.AvatarUrl,
                Bio = user.Bio,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            };
        }
    }
}
