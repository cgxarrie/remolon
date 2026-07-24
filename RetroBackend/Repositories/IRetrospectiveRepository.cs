using RetroBackend.Models;

namespace RetroBackend.Repositories;

public interface IRetrospectiveRepository
{
    Task<IEnumerable<Retrospective>> GetAllAsync();
    Task<IEnumerable<Retrospective>> GetAllForUserAsync(string userId);
    Task<Retrospective?> GetByIdAsync(Guid id);
    Task<Retrospective> AddAsync(Retrospective retrospective);
    Task<Retrospective?> UpdateAsync(Retrospective retrospective);
    Task<bool> DeleteAsync(Guid id);
    Task CopyUserAssignmentsAsync(Guid fromRetrospectiveId, Guid toRetrospectiveId);
}
