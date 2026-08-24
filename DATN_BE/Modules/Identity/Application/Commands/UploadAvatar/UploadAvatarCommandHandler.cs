using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Application.Commands.UploadAvatar
{
    public class UploadAvatarCommandHandler : IRequestHandler<UploadAvatarCommand, string>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICloudinaryService _cloudinaryService;
        public UploadAvatarCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            ICloudinaryService cloudinaryService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _cloudinaryService = cloudinaryService;
        }
        public async Task<string> Handle(UploadAvatarCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("Yêu cầu đăng nhập, người dùng hiện tại chưa đăng nhập");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken) ?? throw new NotFoundException("Không tìm thấy user tương ứng");

            // upload ảnh lên cloudinary
            var avatarUrl = await _cloudinaryService.UploadAvatarAsync(request.File, user.Id.ToString(), cancellationToken);

            // lưu đường dẫn URL ảnh mới vào database
            user.AvatarUrl = avatarUrl;

            await _context.SaveChangesAsync(cancellationToken);

            return avatarUrl;
        }
    }
}
