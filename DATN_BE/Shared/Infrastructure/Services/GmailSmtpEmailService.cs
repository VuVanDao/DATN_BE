using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DATN_BE.Shared.Infrastructure.Services
{
    public class GmailSmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GmailSmtpEmailService> _logger;
        public GmailSmtpEmailService(IConfiguration configuration, ILogger<GmailSmtpEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }
        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default) 
        {
            var host = _configuration["SmtpSettings:Server"] ?? "smtp.gmail.com";
            var port = int.Parse(_configuration["SmtpSettings:Port"] ?? "587");
            var senderName = _configuration["SmtpSettings:SenderName"] ?? "ManagementTask App";
            var senderEmail = _configuration["SmtpSettings:SenderEmail"] ?? _configuration["SmtpSettings:Username"]!;
            var username = _configuration["SmtpSettings:Username"];
            var password = _configuration["SmtpSettings:Password"];
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("Chưa cấu hình tài khoản Gmail SMTP trong appsettings.json.");
                return;
            }
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody,
            };
            message.Body = bodyBuilder.ToMessageBody();
            using var client = new SmtpClient();
            try
            {
                // Kết nối tới Gmail SMTP với giao thức StartTls
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, cancellationToken);
                await client.AuthenticateAsync(username, password, cancellationToken);
                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);
                _logger.LogInformation("Gửi email thành công tới {ToEmail} qua Gmail SMTP.", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email qua Gmail SMTP tới {ToEmail}", toEmail);
                throw new BadRequestException("Không thể gửi email xác nhận. Vui lòng kiểm tra lại cấu hình hoặc thử lại sau.");
            }
        }
        public async Task SendForgotPasswordEmailAsync(string toEmail, string username, string resetToken, CancellationToken cancellationToken = default)
        {
            var subject = "Khôi phục mật khẩu tài khoản của bạn";
            var htmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f4f5f7; margin: 0; padding: 20px; }}
                        .container {{ max-width: 580px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.05); }}
                        .header {{ background-color: #0052cc; padding: 30px 20px; text-align: center; color: #ffffff; }}
                        .header h1 {{ margin: 0; font-size: 24px; font-weight: 700; }}
                        .content {{ padding: 30px; color: #172b4d; line-height: 1.6; font-size: 16px; }}
                        .token-box {{ background-color: #ebecf0; padding: 15px; border-radius: 6px; font-family: monospace; font-size: 18px; font-weight: bold; text-align: center; letter-spacing: 2px; color: #0052cc; margin: 20px 0; word-break: break-all; }}
                        .footer {{ background-color: #fafbfc; padding: 20px; text-align: center; color: #6b778c; font-size: 13px; border-top: 1px solid #ebecf0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>ManagementTask</h1>
                        </div>
                        <div class='content'>
                            <p>Xin chào <strong>{username}</strong>,</p>
                            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản liên kết với địa chỉ email này.</p>
                            <p>Vui lòng sử dụng <strong>Mã xác nhận bảo mật</strong> dưới đây để đặt lại mật khẩu của bạn:</p>
            
                            <div class='token-box'>{resetToken}</div>
            
                            <p style='color: #de350b; font-size: 14px;'><em>* Lưu ý: Mã này chỉ có hiệu lực trong vòng <strong>5 phút</strong>.</em></p>
                            <p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này. Tài khoản của bạn vẫn được an toàn.</p>
                            <br>
                            <p>Trân trọng,<br><strong>Đội ngũ ManagementTask</strong></p>
                        </div>
                        <div class='footer'>
                            <p>© 2026 ManagementTask Project. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";
            await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
        }
    }
}
