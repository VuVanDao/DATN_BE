using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Modules.Identity.Application.Interfaces;
using DATN_BE.Modules.Security.Domain.Entities;
using DATN_BE.Shared.Application.Events;
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
        private readonly IMediator _mediator;
        public LoginCommandHandler(IApplicationDbContext context, IJwtTokenGenerator jwtTokenGenerator, IMediator mediator)
        {
            _context = context;
            _jwtTokenGenerator = jwtTokenGenerator;
            _mediator = mediator;
        }   
        public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm User theo Email kèm Role
            var user = await _context.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);

            // 2. Kiểm tra tài khoản và mật khẩu
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                // Bắn Event thông báo đăng nhập thất bại (An toàn với user?.Id kể cả khi user = null)
                await _mediator.Publish(new UserLoginAttemptedEvent(
                   user?.Id, request.Email, request.IpAddress, request.UserAgent, false, "Email hoặc mật khẩu không chính xác."), cancellationToken);
                throw new BadRequestException("Email hoặc mật khẩu không chính xác.");
            }
            // 3. Kiểm tra trạng thái hoạt động
            if (!user.IsActive)
            {
                // Bắn Event thông báo tài khoản bị khóa
                await _mediator.Publish(new UserLoginAttemptedEvent(
                   user.Id, request.Email, request.IpAddress, request.UserAgent, false, "Tài khoản đang bị khóa."), cancellationToken);
                throw new BadRequestException("Tài khoản của bạn hiện đang bị khóa. Vui lòng liên hệ quản trị viên.");
            }

            // 4. Lấy tên Role
            var role = user.Role?.Name ?? "User";

            // 5. Sinh JWT Token
            var token = _jwtTokenGenerator.GenerateToken(user, role);

            // Sinh Refresh Token và lưu vào DB
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken(user.Id);

            _context.RefreshTokens.Add(refreshToken);

            // 6. Bắn Event thông báo đăng nhập THÀNH CÔNG
            await _mediator.Publish(new UserLoginAttemptedEvent(
                user.Id, request.Email, request.IpAddress, request.UserAgent, true, null), cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return new AuthResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                AvatarUrl = user.AvatarUrl,
                Token = token,
                Role = role,
                IsActive = user.IsActive,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt
            };
        }
    }
}
