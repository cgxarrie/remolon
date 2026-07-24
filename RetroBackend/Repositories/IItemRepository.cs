using RetroBackend.Models;

namespace RetroBackend.Repositories;

public interface IItemRepository
{
    Task<Item> AddAsync(Item item);
    Task<Item?> GetByIdAsync(Guid id);
    Task<IEnumerable<Item>> GetByColumnAsync(Guid columnId);
    Task<IEnumerable<Item>> GetByGroupIdAsync(Guid groupId);
    Task<IEnumerable<ActionItem>> GetActionItemsByColumnAsync(Guid columnId);
    Task<Item?> UpdateAsync(Item item);
    Task UpdateRangeAsync(IEnumerable<Item> items);
    Task<bool> DeleteAsync(Guid id);
}
