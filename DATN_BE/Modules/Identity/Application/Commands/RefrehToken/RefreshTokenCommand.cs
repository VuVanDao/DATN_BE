using DATN_BE.Modules.Identity.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Identity.Application.Commands.RefrehToken
{
    public class RefreshTokenCommand : IRequest<AuthResponseDto>
    {
        public string RefreshToken { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
    }
}
