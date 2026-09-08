using Microsoft.AspNetCore.SignalR;
using RetroBackend.Dtos;
using RetroBackend.Hubs;

namespace RetroBackend.Services;

public sealed class RetrospectiveHubMembership : IRetrospectiveHubMembership
{
    private readonly IHubContext<RetrospectiveHub> _hub;
    private readonly RetrospectiveHubConnectionTracker _tracker;

    public RetrospectiveHubMembership(
        IHubContext<RetrospectiveHub> hub,
        RetrospectiveHubConnectionTracker tracker)
    {
        _hub = hub;
        _tracker = tracker;
    }

    public async Task RemoveUserFromRetrospectiveAsync(
        string userId,
        Guid retrospectiveId,
        CancellationToken cancellationToken = default)
    {
        var group = RetrospectiveHub.GroupName(retrospectiveId);
        foreach (var connectionId in _tracker.GetConnectionIds(userId))
        {
            await _hub.Groups.RemoveFromGroupAsync(connectionId, group, cancellationToken);
            _tracker.ClearRetrospective(connectionId, retrospectiveId);
        }

        await _hub.Clients.User(userId).SendAsync(
            "RetrospectiveAccessRevoked",
            new ItemsChangedDto(retrospectiveId),
            cancellationToken);
    }
}
