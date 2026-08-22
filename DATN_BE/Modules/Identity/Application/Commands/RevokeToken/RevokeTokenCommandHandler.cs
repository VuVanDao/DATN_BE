using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Application.Commands.RevokeToken
{
    public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        public RevokeTokenCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);
            if (token == null)
            {
                throw new NotFoundException("Refresh token không tồn tại.");
            }
            if (!token.IsActive)
            {
                throw new BadRequestException("Refresh token không còn hiệu lực.");
            }
            token.RevokedAt = DateTimeOffset.UtcNow;
            token.RevokedByIp = request.IpAddress;
            token.ReasonRevoked = "Người dùng chủ động đăng xuất (Revoked by user)";

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
