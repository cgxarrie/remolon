using System.Security.Claims;
using RetroBackend.Auth;
using RetroBackend.Dtos;

namespace RetroBackend.Services;

public class RetrospectiveRealtimeService : IRetrospectiveRealtimeService
{
    private readonly IRetrospectiveService _retrospectiveService;
    private readonly IRetroAuthorizationService _authzService;

    public RetrospectiveRealtimeService(
        IRetrospectiveService retrospectiveService,
        IRetroAuthorizationService authzService)
    {
        _retrospectiveService = retrospectiveService;
        _authzService = authzService;
    }

    public async Task<bool> CanAccessAsync(ClaimsPrincipal user, Guid retrospectiveId)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return false;

        var retro = await _retrospectiveService.GetByIdAsync(retrospectiveId);
        if (retro is null) return false;

        var isOwner = await _authzService.IsRetrospectiveOwnerAsync(userId, retrospectiveId);
        var isAssigned = await _authzService.IsAssignedToRetrospectiveAsync(userId, retrospectiveId);
        if (!isOwner && !isAssigned) return false;

        if (!Guid.TryParse(user.FindFirstValue(AuthClaims.OrganizationId), out var organizationId)
            || organizationId != retro.OrganizationId)
        {
            return false;
        }

        return true;
    }

    public async Task<ObjectThrownDto?> TryCreateThrowAsync(
        ClaimsPrincipal user,
        Guid retrospectiveId,
        string targetUserId,
        string objectId)
    {
        if (!await CanAccessAsync(user, retrospectiveId)) return null;

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return null;
        if (string.IsNullOrWhiteSpace(targetUserId)) return null;
        if (string.Equals(userId, targetUserId, StringComparison.OrdinalIgnoreCase)) return null;
        if (!ThrowableObjects.Allowed.Contains(objectId)) return null;

        return new ObjectThrownDto(userId, targetUserId, objectId.ToLowerInvariant());
    }
}
