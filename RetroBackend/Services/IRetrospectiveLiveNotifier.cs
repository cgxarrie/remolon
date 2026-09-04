namespace RetroBackend.Services;

public interface IRetrospectiveLiveNotifier
{
    Task NotifyItemsChangedAsync(Guid retrospectiveId, CancellationToken cancellationToken = default);
    Task NotifyRetrospectiveRevealedAsync(Guid retrospectiveId, CancellationToken cancellationToken = default);
    Task NotifyRetrospectiveClosedAsync(Guid retrospectiveId, CancellationToken cancellationToken = default);
    Task NotifyRetrospectiveDeletedAsync(Guid retrospectiveId, CancellationToken cancellationToken = default);
}
