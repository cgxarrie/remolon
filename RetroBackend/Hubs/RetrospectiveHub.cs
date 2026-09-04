using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RetroBackend.Services;

namespace RetroBackend.Hubs;

[Authorize]
public class RetrospectiveHub : Hub
{
    private readonly IRetrospectiveRealtimeService _realtime;

    public RetrospectiveHub(IRetrospectiveRealtimeService realtime)
    {
        _realtime = realtime;
    }

    public async Task Join(Guid retrospectiveId)
    {
        if (Context.User is null || !await _realtime.CanAccessAsync(Context.User, retrospectiveId))
            throw new HubException("Forbidden");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(retrospectiveId));
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
