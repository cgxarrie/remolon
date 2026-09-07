using RetroBackend.Dtos;
using RetroBackend.Models;

namespace RetroBackend.Services;

public interface IAuthTokenService
{
    Task<AuthTokenResponse> BuildAuthResponseAsync(AppUser user, string role);
}
