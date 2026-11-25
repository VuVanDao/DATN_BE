using MediatR;

namespace DATN_BE.Modules.Identity.Application.Commands.AdminDeleteUser
{
    public class AdminDeleteUserCommand : IRequest<bool>
    {
        /// <summary>
        /// Được gán từ route parameter {id:guid} trong Controller
        /// </summary>
        public Guid UserId { get; set; }
    }
}
