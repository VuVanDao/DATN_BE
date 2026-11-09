using BCrypt.Net;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Application.Commands.ChangePassword
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public ChangePasswordCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("Yêu cầu đăng nhập, người dùng chưa đăng nhập");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken) ?? throw new NotFoundException("Không tìm thấy user tương ứng");
            // kiểm tra mật khẩu hiện tại có đúng không
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                throw new BadRequestException("Mật khẩu hiện tại không chính xác.");
            }
            // cập nhật mật khẩu đã được Hash
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            // 3. THU HỒI TOÀN BỘ REFRESH TOKEN ĐANG HOẠT ĐỘNG (Bảo mật: bắt hacker/thiết bị khác đăng nhập lại)
            var activeRefreshToken = await _context.RefreshTokens
                .Where(rf => rf.UserId == userId && rf.RevokedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var token in activeRefreshToken)
            {
                token.RevokedAt = DateTimeOffset.UtcNow;
                token.ReasonRevoked = "Người dùng đã đổi mật khẩu (Password changed)";
            }
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
