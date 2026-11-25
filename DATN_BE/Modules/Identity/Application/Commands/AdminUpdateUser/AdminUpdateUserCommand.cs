using DATN_BE.Modules.Identity.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Identity.Application.Commands.AdminUpdateUser
{
    public class AdminUpdateUserCommand : IRequest<UserManagementDto>
    {
        /// <summary>
        /// Được gán từ route parameter {id:guid} trong Controller
        /// </summary>
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Bio { get; set; }
    }
}
