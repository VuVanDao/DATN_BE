using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Modules.Identity.Application.Interfaces;
using DATN_BE.Modules.Security.Domain.Entities;
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
                // Ghi log thất bại (Sai email hoặc mật khẩu)
                await LogLoginAttemptAsync(user.Id, request.Email, request.IpAddress, request.UserAgent, false, "Email hoặc mật khẩu không chính xác.", cancellationToken);
                throw new BadRequestException("Email hoặc mật khẩu không chính xác.");
            }
            // 3. Kiểm tra trạng thái hoạt động
            if (!user.IsActive)
            {
                // Ghi log thất bại (Tài khoản bị khóa)
                await LogLoginAttemptAsync(user.Id, request.Email, request.IpAddress, request.UserAgent, false, "Tài khoản đang bị khóa.", cancellationToken);
                throw new BadRequestException("Tài khoản của bạn hiện đang bị khóa. Vui lòng liên hệ quản trị viên.");
            }

            // 4. Lấy danh sách tên Roles
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

            // 5. Sinh JWT Token
            var token = _jwtTokenGenerator.GenerateToken(user, roles);

            // Sinh Refresh Token và lưu vào DB
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken(user.Id);

            _context.RefreshTokens.Add(refreshToken);

            // 6. Ghi log thành công
            await LogLoginAttemptAsync(user.Id, request.Email, request.IpAddress, request.UserAgent, true, null, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return new AuthResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                AvatarUrl = user.AvatarUrl,
                Token = token,
                Roles = roles,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt
            };
        }

        private async System.Threading.Tasks.Task LogLoginAttemptAsync(Guid id, string email, string? ipAddress, string? userAgent, bool isSuccess, string? failureReason, CancellationToken cancellationToken)
        {
            var log = new LoginHistory
            {
                UserId = id,
                EmailAttempted = email,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccess = isSuccess,
                FailureReason = failureReason,
            };
            _context.LoginHistories.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
