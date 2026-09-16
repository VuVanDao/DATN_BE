using MediatR;

namespace DATN_BE.Modules.Identity.Application.Commands.AdminCreateUser
{
    public class AdminCreateUserCommand : IRequest<Guid>
    {
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Bio { get; set; }
    }
}
