using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Application.Commands.UpdateUserStatus
{
    public class UpdateUserStatusCommandHandler : IRequestHandler<UpdateUserStatusCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public UpdateUserStatusCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        public async Task<bool> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra an toàn: Không cho phép Admin tự khóa tài khoản của chính mình
            if (_currentUserService.UserId.HasValue && _currentUserService.UserId.Value == request.UserId && !request.IsActive)
            {
                throw new BadRequestException("Bạn không thể tự khóa tài khoản quản trị viên của chính mình.");
            }
            // 2. Tìm User cần cập nhật
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken) ?? throw new NotFoundException("Không tìm thấy người dùng");
            user.IsActive = request.IsActive;
            // 3. Nếu KHÓA tài khoản (IsActive = false): Lập tức thu hồi TOÀN BỘ Refresh Token của user này
            if (!request.IsActive)
            {
                var activeTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
                    .ToListAsync(cancellationToken);

                foreach (var token in activeTokens)
                {
                    token.RevokedAt = DateTimeOffset.UtcNow;
                    token.ReasonRevoked = string.IsNullOrWhiteSpace(request.Reason)
                        ? "Tài khoản bị khóa bởi quản trị viên"
                        : $"Tài khoản bị khóa: {request.Reason}";
                }
            }
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
