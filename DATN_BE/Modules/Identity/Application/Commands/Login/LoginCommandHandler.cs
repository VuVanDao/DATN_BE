using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Modules.Identity.Application.Interfaces;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Application.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        public LoginCommandHandler(IApplicationDbContext context, IJwtTokenGenerator jwtTokenGenerator)
        {
            _context = context;
            _jwtTokenGenerator = jwtTokenGenerator;
        }
        public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm User theo Email kèm danh sách Roles
            var user = await _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && !u.IsDeleted, cancellationToken);
            // 2. Kiểm tra tài khoản và mật khẩu
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new BadRequestException("Email hoặc mật khẩu không chính xác.");
            }
            // 3. Kiểm tra trạng thái hoạt động
            if (!user.IsActive)
            {
                throw new BadRequestException("Tài khoản của bạn hiện đang bị khóa. Vui lòng liên hệ quản trị viên.");
            }
            // 4. Lấy danh sách tên Roles
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            // 5. Sinh JWT Token
            var token = _jwtTokenGenerator.GenerateToken(user, roles);

            return new AuthResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                AvatarUrl = user.AvatarUrl,
                Token = token,
                Roles = roles
            };
        }
    }
}
