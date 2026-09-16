using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Application.Queries.GetProfile
{
    public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, UserProfileDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public GetProfileQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        public async Task<UserProfileDto> Handle(GetProfileQuery request, CancellationToken cancellationToken)
        {
            var UserId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("Yêu cầu đăng nhập, bạn chưa đăng nhập vào hệ thống");
            // tìm user tương ứng với UserId
            var user = await _context.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == UserId, cancellationToken) ?? throw new NotFoundException("Không tìm thấy người dùng");

            return new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                AvatarUrl = user.AvatarUrl,
                Bio = user.Bio,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Role = user.Role?.Name ?? "User"
            };
        }
    }
}
