using DATN_BE.Modules.Identity.Domain.Entities;

namespace DATN_BE.Modules.Identity.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user, string role);
        RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress = null);
    }
}
