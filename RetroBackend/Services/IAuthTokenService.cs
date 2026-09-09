using RetroBackend.Dtos;
using RetroBackend.Models;

namespace RetroBackend.Services;

public interface IAuthTokenService
{
    Task<AuthTokenResponse> BuildAuthResponseAsync(AppUser user, string role);
    Task<AuthTokenResponse?> RefreshAsync(string refreshToken);
    Task RevokeAllForUserAsync(string userId);
    Task RevokeAsync(string refreshToken);
}
