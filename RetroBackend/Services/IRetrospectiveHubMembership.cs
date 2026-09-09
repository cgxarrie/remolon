namespace RetroBackend.Services;

public interface IRetrospectiveHubMembership
{
    Task RemoveUserFromRetrospectiveAsync(
        string userId,
        Guid retrospectiveId,
        CancellationToken cancellationToken = default);
}
