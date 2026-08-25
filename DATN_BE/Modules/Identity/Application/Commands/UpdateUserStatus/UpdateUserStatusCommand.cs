using MediatR;

namespace DATN_BE.Modules.Identity.Application.Commands.UpdateUserStatus
{
    public class UpdateUserStatusCommand : IRequest<bool>
    {
        public Guid UserId { get; set; }
        public bool IsActive { get; set; }
        public string? Reason { get; set; }
    }
}
