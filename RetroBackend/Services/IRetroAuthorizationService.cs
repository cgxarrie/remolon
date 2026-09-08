namespace RetroBackend.Services;

public interface IRetroAuthorizationService
{
    Task<bool> IsAssignedToRetrospectiveAsync(string userId, Guid retrospectiveId);
    Task<bool> IsAssignedToRetrospectiveByColumnAsync(string userId, Guid columnId);
    Task<bool> IsItemOwnerAsync(string userId, Guid itemId);
    Task<bool> IsAssignedToRetrospectiveByItemAsync(string userId, Guid itemId);
    Task<bool> IsRetrospectiveOwnerAsync(string userId, Guid retrospectiveId);
    Task<bool> IsRetrospectiveOwnerByColumnAsync(string userId, Guid columnId);
    Task<bool> IsRetrospectiveOwnerByItemAsync(string userId, Guid itemId);
    Task<bool> IsRetrospectiveRevealedByItemAsync(Guid itemId);
    Task<bool> IsRetrospectiveClosedByItemAsync(Guid itemId);
    Task<Guid?> GetRetrospectiveIdByColumnAsync(Guid columnId);
    Task<Guid?> GetRetrospectiveIdByItemAsync(Guid itemId);
}
