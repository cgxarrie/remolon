using RetroBackend.Services;

namespace RetroBackend.Tests.Fakes;

public sealed class NoOpHubMembership : IRetrospectiveHubMembership
{
    public Task RemoveUserFromRetrospectiveAsync(
        string userId,
        Guid retrospectiveId,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

public sealed class RecordingHubMembership : IRetrospectiveHubMembership
{
    public List<(string UserId, Guid RetrospectiveId)> Removals { get; } = [];

    public Task RemoveUserFromRetrospectiveAsync(
        string userId,
        Guid retrospectiveId,
        CancellationToken cancellationToken = default)
    {
        Removals.Add((userId, retrospectiveId));
        return Task.CompletedTask;
    }
}
