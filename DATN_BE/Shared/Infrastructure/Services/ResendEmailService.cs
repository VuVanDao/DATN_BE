using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DATN_BE.Shared.Infrastructure.Services
{
    public class ResendEmailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ResendEmailService> _logger;
        public ResendEmailService(HttpClient httpClient, IConfiguration configuration, ILogger<ResendEmailService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["ResendSettings:ApiKey"];
            var fromEmail = _configuration["ResendSettings:FromEmail"] ?? "onboarding@resend.dev";
            var fromName = _configuration["ResendSettings:FromName"] ?? "ManagementTask App";

            if (string.IsNullOrEmpty(apiKey) || apiKey.StartsWith("re_YOUR_"))
            {
                _logger.LogWarning("Chưa cấu hình API Key hợp lệ cho Resend. Email tới {ToEmail} sẽ không được gửi.", toEmail);
                return;
            }
            var requestPayload = new
            {
                from = $"{fromName} <{fromEmail}>",
                to = new[] { toEmail },
                subject = subject,
                html = htmlBody
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
            {
                Content = jsonContent
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Gửi email qua Resend thất bại. StatusCode: {response.StatusCode}, Error: {responseBody}");
                    throw new BadRequestException($"Không thể gửi email: {responseBody}");
                }
                _logger.LogInformation("Gửi email thành công tới {ToEmail} qua Resend.", toEmail);
            }
            catch(Exception ex) when (ex is not BadRequestException)
            {
                _logger.LogError(ex, "Đã xảy ra lỗi khi kết nối tới Resend API.");
                throw new BadRequestException("Đã xảy ra sự cố trong quá trình gửi email. Vui lòng thử lại sau.");
            }
        }

        public async Task SendForgotPasswordEmailAsync(string toEmail, string username, string resetToken, CancellationToken cancellationToken = default)
        {
            var subject = "Khôi phục mật khẩu tài khoản của bạn";
            // Template HTML chuẩn Responsive
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
