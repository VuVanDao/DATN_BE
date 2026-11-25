using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Modules.Identity.Application.Interfaces;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Application.Commands.RefrehToken
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        public RefreshTokenCommandHandler(IApplicationDbContext context, IJwtTokenGenerator jwtTokenGenerator)
        {
            _context = context;
            _jwtTokenGenerator = jwtTokenGenerator;
        }
        public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm Refresh Token trong DB kèm thông tin User và Role
            var existingToken = await _context.RefreshTokens.Include(rf => rf.User).ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(rf => rf.Token ==  request.RefreshToken,cancellationToken);
            if(existingToken == null)
            {
                throw new NotFoundException("Refresh token không hợp lệ");
            }
            // 2. PHÁT HIỆN DÙNG LẠI TOKEN CŨ (Reuse Detection):
            // Nếu token đã bị thu hồi và đã có Token thay thế
            if (existingToken.IsRevoked)
            {
                // Thu hồi tất cả các token
                var allActiveTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == existingToken.UserId && rt.RevokedAt == null)
                    .ToListAsync(cancellationToken);
                foreach (var token in allActiveTokens)
                {
                    token.RevokedAt = DateTimeOffset.UtcNow;
                    token.RevokedByIp = request.IpAddress;
                    token.ReasonRevoked = $"Phát hiện hành vi dùng lại token đã bị thay thế: {existingToken.Token}";
                }
                await _context.SaveChangesAsync(cancellationToken);
                throw new BadRequestException("Phát hiện bất thường bảo mật. Toàn bộ phiên đăng nhập đã bị hủy. Vui lòng đăng nhập lại.");
            }
            // 3. Kiểm tra hết hạn hoặc tài khoản bị khóa
            if (existingToken.IsExpired)
            {
                throw new BadRequestException("Refresh token đã hết hạn. Vui lòng đăng nhập lại.");
            }
            if (!existingToken.User.IsActive || existingToken.User.IsDeleted)
            {
                throw new BadRequestException("Tài khoản của bạn hiện đang bị khóa hoặc đã bị xóa.");
            }
            // 4. TOKEN ROTATION: Tạo Refresh Token mới và thu hồi Token cũ
            var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken(existingToken.UserId, request.IpAddress);

            existingToken.RevokedAt = DateTimeOffset.UtcNow;
            existingToken.RevokedByIp = request.IpAddress;
            existingToken.ReplacedByToken = newRefreshToken.Token;
            existingToken.ReasonRevoked = "Đổi token mới thành công (Rotated)";

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync(cancellationToken);

            // 5. Sinh JWT Access Token mới
            var role = existingToken.User.Role?.Name ?? RoleConstants.User;
            var newAccessToken = _jwtTokenGenerator.GenerateToken(existingToken.User, role);

            return new AuthResponseDto
            {
                Id = existingToken.User.Id,
                Email = existingToken.User.Email,
                Username = existingToken.User.Username,
                AvatarUrl = existingToken.User.AvatarUrl,
                Token = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                RefreshTokenExpiresAt = newRefreshToken.ExpiresAt,
                Role = role
            };
        }
    }
}
