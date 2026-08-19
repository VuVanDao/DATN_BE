using DATN_BE.Modules.Identity.Domain.Entities;

namespace DATN_BE.Modules.Identity.Application.Interfaces
{
    public interface IJwtGenerator
    {
        string GenerateToken(User user, IEnumerable<string> roles);
    }
}
