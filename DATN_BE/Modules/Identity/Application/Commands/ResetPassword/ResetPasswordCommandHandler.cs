using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Application.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        public ResetPasswordCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var email = request.Email.Trim().ToLower();
            // 1. Tìm User theo Email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower() && u.IsActive,cancellationToken) ?? throw new NotFoundException("Yêu cầu đặt lại mật khẩu không hợp lệ hoặc tài khoản không khả dụng");
            // 2. Tìm Token xác nhận
            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(prt => prt.Token == request.Token && prt.UserId == user.Id, cancellationToken)
                ?? throw new NotFoundException("Mã xác nhận không hợp lệ.");
            if (resetToken.IsUsed)
            {
                throw new BadRequestException("Mã xác nhận này đã được sử dụng.");
            }
            if (resetToken.IsExpired)
            {
                throw new BadRequestException("Mã xác nhận đã hết hạn (chỉ có hiệu lực trong 5 phút). Vui lòng yêu cầu mã mới.");
            }
            // 3. Cập nhật mật khẩu mới (Băm BCrypt)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            // 4. Đánh dấu Token đã sử dụng
            resetToken.IsUsed = true;
            resetToken.UsedAt = DateTimeOffset.UtcNow;
            // 5. Thu hồi tất cả RefreshToken đang hoạt động của User này để bảo mật
            var activeRefreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
                .ToArrayAsync(cancellationToken);
            foreach (var rt in activeRefreshTokens)
            {
                rt.RevokedAt = DateTimeOffset.UtcNow;
                rt.ReasonRevoked = "Đặt lại mật khẩu qua Forgot Password";
            }
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
