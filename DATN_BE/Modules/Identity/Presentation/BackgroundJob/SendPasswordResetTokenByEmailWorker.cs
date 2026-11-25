using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Presentation.BackgroundJob
{
    public class SendPasswordResetTokenByEmailWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SendPasswordResetTokenByEmailWorker> _logger;
        public SendPasswordResetTokenByEmailWorker(IServiceScopeFactory scopeFactory,
        ILogger<SendPasswordResetTokenByEmailWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }
        protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOneAsync(stoppingToken);
                }catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xử lý hàng đợi email đặt lại mật khẩu");
                }
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        private async System.Threading.Tasks.Task ProcessOneAsync(CancellationToken stoppingToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var now = DateTimeOffset.UtcNow;
            var job = await db.PasswordResetEmailJob
                    .Where(j => j.Status == StatusSendEmail.Pending && (j.NextAttemptAt == null || j.NextAttemptAt <= now))
                    .OrderBy(j => j.CreatedAt)
                    .FirstOrDefaultAsync(stoppingToken);

            if (job is null) return;

            var token = await db.PasswordResetTokens
                    .FirstOrDefaultAsync(t => t.Id == job.PasswordResetTokenId, stoppingToken);

            if (token is null || token.IsUsed || token.ExpiresAt <= now)
            {
                job.Status = StatusSendEmail.Failed;
                job.LastError = "Token không còn hiệu lực";
                await db.SaveChangesAsync(stoppingToken);
                return;
            }

            var user = await db.Users
                    .FirstOrDefaultAsync(u => u.Id == token.UserId, stoppingToken);

            if (user is null || !user.IsActive)
            {
                job.Status = StatusSendEmail.Failed;
                job.LastError = "Tài khoản không khả dụng";
                await db.SaveChangesAsync(stoppingToken);
                return;
            }
            job.Attempts++;
            try
            {
                await emailService.SendForgotPasswordEmailAsync(
                    job.ToEmail, user.Username, token.Token, stoppingToken);

                job.Status = StatusSendEmail.Sent;
                job.SentAt = DateTimeOffset.UtcNow;
                job.LastError = string.Empty;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Gửi email cho job {JobId} thất bại", job.Id);
                job.LastError = ex.Message[..Math.Min(ex.Message.Length, 2000)];

                var retryAt = DateTimeOffset.UtcNow.AddSeconds(30 * job.Attempts);
                if (job.Attempts >= 3 || retryAt >= token.ExpiresAt)
                    job.Status = StatusSendEmail.Failed;
                else
                    job.NextAttemptAt = retryAt;
            }
            await db.SaveChangesAsync(stoppingToken);
        }
    }
}
