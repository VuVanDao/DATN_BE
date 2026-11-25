using MediatR;

namespace DATN_BE.Modules.Identity.Application.Commands.ForgotPassword
{
    public class ForgotPasswordCommand : IRequest<string>
    {
        public string Email { get; set; } = string.Empty;
    }
}
