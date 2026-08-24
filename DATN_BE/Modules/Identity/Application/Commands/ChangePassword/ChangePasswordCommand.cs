using MediatR;

namespace DATN_BE.Modules.Identity.Application.Commands.ChangePassword
{
    public class ChangePasswordCommand : IRequest<bool>
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
