using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Application.Commands.AdminCreateUser
{
    public class AdminCreateUserCommandHandler : IRequestHandler<AdminCreateUserCommand, Guid>
    {
        private readonly IApplicationDbContext _context;

        public AdminCreateUserCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(AdminCreateUserCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra Email đã tồn tại chưa
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);
            if (emailExists)
            {
                throw new BadRequestException("Địa chỉ email này đã được sử dụng.");
            }

            // 2. Kiểm tra Username đã tồn tại chưa
            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username.ToLower() == request.Username.ToLower(), cancellationToken);
            if (usernameExists)
            {
                throw new BadRequestException($"Username '{request.Username}' đã được sử dụng.");
            }

            // 3. Băm mật khẩu bằng BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 4. Lấy vai trò mặc định "User" (Global)
            var defaultRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "User", cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy vai trò mặc định 'User' trong hệ thống.");

            // 5. Khởi tạo Entity User
            var user = new User
            {
                Email = request.Email.Trim().ToLower(),
                Username = request.Username.Trim(),
                PasswordHash = passwordHash,
                Bio = request.Bio?.Trim(),
                IsActive = true,
                RoleId = defaultRole.Id
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            return user.Id;
        }
    }
}
