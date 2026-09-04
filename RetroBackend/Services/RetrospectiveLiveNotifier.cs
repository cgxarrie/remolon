using Microsoft.AspNetCore.SignalR;
using RetroBackend.Dtos;
using RetroBackend.Hubs;

namespace RetroBackend.Services;

public class RetrospectiveLiveNotifier : IRetrospectiveLiveNotifier
{
    private readonly IHubContext<RetrospectiveHub> _hub;

    public RetrospectiveLiveNotifier(IHubContext<RetrospectiveHub> hub)
    {
        _hub = hub;
    }

    public Task NotifyItemsChangedAsync(Guid retrospectiveId, CancellationToken cancellationToken = default) =>
        _hub.Clients
            .Group(RetrospectiveHub.GroupName(retrospectiveId))
            .SendAsync("ItemsChanged", new ItemsChangedDto(retrospectiveId), cancellationToken);

    public Task NotifyRetrospectiveRevealedAsync(Guid retrospectiveId, CancellationToken cancellationToken = default) =>
        Send(retrospectiveId, "RetrospectiveRevealed", cancellationToken);

    public Task NotifyRetrospectiveClosedAsync(Guid retrospectiveId, CancellationToken cancellationToken = default) =>
        Send(retrospectiveId, "RetrospectiveClosed", cancellationToken);

    public Task NotifyRetrospectiveDeletedAsync(Guid retrospectiveId, CancellationToken cancellationToken = default) =>
        Send(retrospectiveId, "RetrospectiveDeleted", cancellationToken);

    private Task Send(Guid retrospectiveId, string method, CancellationToken cancellationToken) =>
        _hub.Clients
            .Group(RetrospectiveHub.GroupName(retrospectiveId))
            .SendAsync(method, new ItemsChangedDto(retrospectiveId), cancellationToken);
}
