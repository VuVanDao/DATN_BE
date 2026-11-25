using MediatR;

namespace DATN_BE.Modules.Identity.Application.Commands.UploadAvatar
{
    public class UploadAvatarCommand : IRequest<string>
    {
        public IFormFile File { get; set; } = null!;
    }
}
