using DATN_BE.Modules.Identity.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Identity.Application.Commands.UpdateProfile
{
    public class UpdateProfileCommand : IRequest<UserProfileDto>
    {
        public string Username { get; set; } = string.Empty;
        public string? Bio { get; set; }
    }
}
