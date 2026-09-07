namespace DATN_BE.Shared.Application.Interfaces
{
    public interface IEmailService
    {
        /// <summary>
        /// Gửi email thô với nội dung HTML
        /// </summary>
        Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gửi email đặt lại mật khẩu kèm mẫu Template HTML
        /// </summary>
        Task SendForgotPasswordEmailAsync(string toEmail, string username, string resetToken, CancellationToken cancellationToken = default);
    }
}
