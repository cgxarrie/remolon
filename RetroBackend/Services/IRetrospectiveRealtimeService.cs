using System.Security.Claims;
using RetroBackend.Dtos;

namespace RetroBackend.Services;

public interface IRetrospectiveRealtimeService
{
    Task<bool> CanAccessAsync(ClaimsPrincipal user, Guid retrospectiveId);
    Task<ObjectThrownDto?> TryCreateThrowAsync(
        ClaimsPrincipal user,
        Guid retrospectiveId,
        string targetUserId,
        string objectId);
}
