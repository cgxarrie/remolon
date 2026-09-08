using Microsoft.AspNetCore.Identity;

namespace RetroBackend.Auth;

public static class IdentityPasswordPolicy
{
    public const int RequiredLength = 12;

    public static void Apply(IdentityOptions options)
    {
        options.Password.RequiredLength = RequiredLength;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
    }
}
