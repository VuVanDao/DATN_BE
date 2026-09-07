using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace DATN_BE.Modules.Identity.Application.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, string>
    {
        private readonly IApplicationDbContext _context;
        private readonly IEmailService _emailService;
        public ForgotPasswordCommandHandler(IApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<string> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var email = request.Email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email) ?? throw new NotFoundException("Nếu địa chỉ email này tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi");
            if (!user.IsActive)
            {
                throw new BadRequestException("Tài khoản của bạn hiện đang bị khóa. Vui lòng liên hệ quản trị viên");
            }
            // 1. Hủy các token reset cũ chưa dùng của user này
            var existingTokens = await _context.PasswordResetTokens
                .Where(prt => prt.UserId == user.Id && !prt.IsUsed && prt.ExpiresAt > DateTimeOffset.UtcNow)
                .ToListAsync(cancellationToken);

            foreach (var t in existingTokens)
            {
                t.IsUsed = true;
                t.UsedAt = DateTimeOffset.UtcNow;
            }
            // 2. Sinh chuỗi ngẫu nhiên bảo mật 64 bytes dạng Hex string (hoặc Base64)
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var resetTokenString = Convert.ToHexString(tokenBytes).ToLower();
            // 3. Tạo PasswordResetToken mới có hạn trong 15 phút
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
                IsUsed = false,
                Token = resetTokenString,          
            };
            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync(cancellationToken);

            // 4. GỬI EMAIL QUA RESEND ĐẾN NGƯỜI DÙNG
            await _emailService.SendForgotPasswordEmailAsync(email,user.Username,resetToken.Token,cancellationToken);

            return "Hướng dẫn đặt lại mật khẩu đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư.";
        }
    }
}
