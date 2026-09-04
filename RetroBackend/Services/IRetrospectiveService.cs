using RetroBackend.Models;

namespace RetroBackend.Services;

public interface IRetrospectiveService
{
    Task<IEnumerable<Retrospective>> GetAllAsync();
    Task<IEnumerable<Retrospective>> GetAllForUserAsync(string userId);
    Task<Retrospective?> GetByIdAsync(Guid id);
    Task<Retrospective> CreateAsync(CreateRetrospectiveRequest request);
    Task<Retrospective?> UpdateAsync(Guid id, UpdateRetrospectiveRequest request);
    Task<Retrospective?> RevealAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
    Task<Retrospective?> CloseAsync(Guid id, CloseRetrospectiveRequest request);
    Task<Retrospective?> CreateNextIterationAsync(Guid id, string currentUser);
    Task<bool> HasOpenWithTitleAsync(Guid organizationId, string title);
}
