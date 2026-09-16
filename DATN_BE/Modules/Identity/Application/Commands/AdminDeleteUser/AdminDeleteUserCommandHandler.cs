using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Application.Commands.AdminDeleteUser
{
    public class AdminDeleteUserCommandHandler : IRequestHandler<AdminDeleteUserCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AdminDeleteUserCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<bool> Handle(AdminDeleteUserCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra an toàn: Admin không được tự xóa chính mình
            if (_currentUserService.UserId.HasValue && _currentUserService.UserId.Value == request.UserId)
            {
                throw new BadRequestException("Bạn không thể tự xóa tài khoản quản trị viên của chính mình.");
            }

            // 2. Tìm User cần xóa
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy người dùng.");

            // 3. Thu hồi toàn bộ Refresh Token đang hoạt động của user này
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTimeOffset.UtcNow;
                token.ReasonRevoked = "Tài khoản đã bị xóa bởi quản trị viên.";
            }

            // 4. Xóa mềm - EF Core interceptor trong ApplicationDbContext sẽ tự động
            //    chuyển EntityState.Deleted thành EntityState.Modified và gán IsDeleted = true, DeletedAt = now
            _context.Users.Remove(user);

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
