using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Application.Commands.AdminUpdateUser
{
    public class AdminUpdateUserCommandHandler : IRequestHandler<AdminUpdateUserCommand, UserManagementDto>
    {
        private readonly IApplicationDbContext _context;

        public AdminUpdateUserCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserManagementDto> Handle(AdminUpdateUserCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm User cần cập nhật (kèm Role để trả về DTO)
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy người dùng.");

            // 2. Kiểm tra trùng Username nếu admin đổi username mới
            var newUsername = request.Username.Trim();
            if (!string.Equals(user.Username, newUsername, StringComparison.OrdinalIgnoreCase))
            {
                var usernameExists = await _context.Users
                    .AnyAsync(u => u.Username.ToLower() == newUsername.ToLower() && u.Id != user.Id, cancellationToken);
                if (usernameExists)
                {
                    throw new BadRequestException($"Username '{newUsername}' đã được sử dụng bởi tài khoản khác.");
                }
                user.Username = newUsername;
            }

            // 3. Cập nhật Bio
            user.Bio = request.Bio?.Trim();

            await _context.SaveChangesAsync(cancellationToken);

            // 4. Trả về DTO
            return new UserManagementDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                AvatarUrl = user.AvatarUrl,
                Bio = user.Bio,
                IsActive = user.IsActive,
                IsDeleted = user.IsDeleted,
                CreatedAt = user.CreatedAt,
                Role = user.Role?.Name ?? RoleConstants.User
            };
        }
    }
}
