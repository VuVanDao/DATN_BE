using MediatR;

namespace DATN_BE.Modules.Identity.Application.Commands.RevokeToken
{
    public class RevokeTokenCommand : IRequest<bool>
    {
        public string RefreshToken { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
    }
}
