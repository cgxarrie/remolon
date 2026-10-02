using System.Security.Claims;
using RetroBackend.Auth;
using RetroBackend.Data;
using RetroBackend.Dtos;
using RetroBackend.Hubs;
using RetroBackend.Models;

namespace RetroBackend.Services;

public class RetrospectiveRealtimeService : IRetrospectiveRealtimeService
{
    private readonly IRetrospectiveService _retrospectiveService;
    private readonly IRetroAuthorizationService _authzService;
    private readonly RetroDbContext _context;
    private readonly RetrospectiveHubConnectionTracker _connections;

    public RetrospectiveRealtimeService(
        IRetrospectiveService retrospectiveService,
        IRetroAuthorizationService authzService,
        RetroDbContext context,
        RetrospectiveHubConnectionTracker connections)
    {
        _retrospectiveService = retrospectiveService;
        _authzService = authzService;
        _context = context;
        _connections = connections;
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

        var targetOnBoard = await _authzService.IsRetrospectiveOwnerAsync(targetUserId, retrospectiveId)
            || await _authzService.IsAssignedToRetrospectiveAsync(targetUserId, retrospectiveId);
        if (!targetOnBoard) return null;

        var normalizedObjectId = objectId.ToLowerInvariant();
        if (!_connections.IsViewingRetrospective(targetUserId, retrospectiveId))
            await RecordReceivedThrowAsync(targetUserId, normalizedObjectId);

        return new ObjectThrownDto(userId, targetUserId, normalizedObjectId);
    }

    private async Task RecordReceivedThrowAsync(string targetUserId, string objectId)
    {
        var tally = await _context.ReceivedThrows.FindAsync(targetUserId, objectId);
        var now = DateTime.UtcNow;
        if (tally is null)
        {
            _context.ReceivedThrows.Add(new ReceivedThrow
            {
                UserId = targetUserId,
                ObjectId = objectId,
                Count = 1,
                LastThrownAt = now,
            });
        }
        else
        {
            tally.Count++;
            tally.LastThrownAt = now;
        }

        await _context.SaveChangesAsync();
    }
}
