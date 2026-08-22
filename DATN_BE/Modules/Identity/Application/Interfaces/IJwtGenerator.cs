using DATN_BE.Modules.Identity.Domain.Entities;

namespace DATN_BE.Modules.Identity.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user, IEnumerable<string> roles);
        RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress = null);
    }
}
