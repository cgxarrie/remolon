using System.Collections.Concurrent;

namespace RetroBackend.Hubs;

public sealed class RetrospectiveHubConnectionTracker
{
    private readonly ConcurrentDictionary<string, ConnectionState> _byConnection = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _byUser = new();

    public void Add(string connectionId, string userId)
    {
        _byConnection[connectionId] = new ConnectionState(userId, null);
        _byUser.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>())[connectionId] = 0;
    }

    public void Remove(string connectionId)
    {
        if (!_byConnection.TryRemove(connectionId, out var state)) return;
        if (!_byUser.TryGetValue(state.UserId, out var connections)) return;
        connections.TryRemove(connectionId, out _);
        if (connections.IsEmpty)
            _byUser.TryRemove(state.UserId, out _);
    }

    public Guid? GetRetrospective(string connectionId) =>
        _byConnection.TryGetValue(connectionId, out var state) ? state.RetrospectiveId : null;

    public void SetRetrospective(string connectionId, Guid retrospectiveId)
    {
        if (!_byConnection.TryGetValue(connectionId, out var existing)) return;
        _byConnection[connectionId] = existing with { RetrospectiveId = retrospectiveId };
    }

    public void ClearRetrospective(string connectionId, Guid retrospectiveId)
    {
        if (!_byConnection.TryGetValue(connectionId, out var existing)) return;
        if (existing.RetrospectiveId != retrospectiveId) return;
        _byConnection[connectionId] = existing with { RetrospectiveId = null };
    }

    public IReadOnlyList<string> GetConnectionIds(string userId) =>
        _byUser.TryGetValue(userId, out var connections)
            ? connections.Keys.ToArray()
            : [];

    private sealed record ConnectionState(string UserId, Guid? RetrospectiveId);
}
