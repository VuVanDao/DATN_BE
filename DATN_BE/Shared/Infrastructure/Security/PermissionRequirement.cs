using Microsoft.AspNetCore.Authorization;

namespace DATN_BE.Shared.Infrastructure.Security
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }
        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }
}
