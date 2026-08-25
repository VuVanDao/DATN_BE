using MediatR;

namespace DATN_BE.Modules.Identity.Application.Commands.ResetPassword
{
    public class ResetPasswordCommand : IRequest<bool>
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
