using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RetroBackend.Services;

namespace RetroBackend.Hubs;

[Authorize]
public class RetrospectiveHub : Hub
{
    private readonly IRetrospectiveRealtimeService _realtime;
    private readonly RetrospectiveHubConnectionTracker _tracker;

    public RetrospectiveHub(IRetrospectiveRealtimeService realtime, RetrospectiveHubConnectionTracker tracker)
    {
        _realtime = realtime;
        _tracker = tracker;
    }

    public override Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
            _tracker.Add(Context.ConnectionId, userId);

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _tracker.Remove(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task Join(Guid retrospectiveId)
    {
        if (Context.User is null || !await _realtime.CanAccessAsync(Context.User, retrospectiveId))
            throw new HubException("Forbidden");

        var previous = _tracker.GetRetrospective(Context.ConnectionId);
        if (previous is Guid previousId && previousId != retrospectiveId)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(previousId));

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(retrospectiveId));
        _tracker.SetRetrospective(Context.ConnectionId, retrospectiveId);
    }

    public async Task Throw(Guid retrospectiveId, string targetUserId, string objectId)
    {
        if (Context.User is null) return;

        var payload = await _realtime.TryCreateThrowAsync(
            Context.User,
            retrospectiveId,
            targetUserId,
            objectId);
        if (payload is null) return;

        await Clients.OthersInGroup(GroupName(retrospectiveId)).SendAsync("ObjectThrown", payload);
    }

    public static string GroupName(Guid retrospectiveId) => $"retro:{retrospectiveId}";
}
