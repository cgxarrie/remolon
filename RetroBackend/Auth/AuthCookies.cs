using Microsoft.AspNetCore.Http;
using RetroBackend.Dtos;
using RetroBackend.Services;

namespace RetroBackend.Auth;

public static class AuthCookies
{
    public const string Access = "access_token";
    public const string Refresh = "refresh_token";

    public static void Append(HttpResponse response, bool secure, AuthTokenResponse tokens)
    {
        response.Cookies.Append(Access, tokens.Token, AccessOptions(secure));
        if (!string.IsNullOrEmpty(tokens.RefreshToken))
        {
            response.Cookies.Append(Refresh, tokens.RefreshToken, RefreshOptions(secure));
        }
    }

    public static void Clear(HttpResponse response, bool secure)
    {
        response.Cookies.Delete(Access, AccessOptions(secure));
        response.Cookies.Delete(Refresh, RefreshOptions(secure));
    }

    public static CookieOptions AccessOptions(bool secure) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        MaxAge = AuthTokenService.AccessTokenLifetime,
    };

    public static CookieOptions RefreshOptions(bool secure) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Lax,
        Path = "/api/auth",
        MaxAge = AuthTokenService.RefreshTokenLifetime,
    };
}
