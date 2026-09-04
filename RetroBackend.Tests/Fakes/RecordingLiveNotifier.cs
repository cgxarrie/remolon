using RetroBackend.Services;

namespace RetroBackend.Tests.Fakes;

public sealed class RecordingLiveNotifier : IRetrospectiveLiveNotifier
{
    public List<Guid> ItemsChangedNotifications { get; } = [];
    public List<Guid> RevealedNotifications { get; } = [];
    public List<Guid> ClosedNotifications { get; } = [];
    public List<Guid> DeletedNotifications { get; } = [];

    public Task NotifyItemsChangedAsync(Guid retrospectiveId, CancellationToken cancellationToken = default)
    {
        ItemsChangedNotifications.Add(retrospectiveId);
        return Task.CompletedTask;
    }

    public Task NotifyRetrospectiveRevealedAsync(Guid retrospectiveId, CancellationToken cancellationToken = default)
    {
        RevealedNotifications.Add(retrospectiveId);
        return Task.CompletedTask;
    }

    public Task NotifyRetrospectiveClosedAsync(Guid retrospectiveId, CancellationToken cancellationToken = default)
    {
        ClosedNotifications.Add(retrospectiveId);
        return Task.CompletedTask;
    }

    public Task NotifyRetrospectiveDeletedAsync(Guid retrospectiveId, CancellationToken cancellationToken = default)
    {
        DeletedNotifications.Add(retrospectiveId);
        return Task.CompletedTask;
    }
}
