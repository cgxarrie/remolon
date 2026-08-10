using System.Security.Claims;

namespace RetroBackend.Auth;

public static class ClaimsPrincipalRoleExtensions
{
    private static readonly string[] RoleClaimTypes =
    [
        ClaimTypes.Role,
        "role",
        "roles",
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
    ];

    public static bool HasRole(this ClaimsPrincipal user, string role)
    {
        if (user.Identity?.IsAuthenticated != true) return false;

        return user.Claims.Any(c =>
            RoleClaimTypes.Contains(c.Type, StringComparer.OrdinalIgnoreCase)
            && string.Equals(c.Value, role, StringComparison.OrdinalIgnoreCase));
    }
}
