using RetroBackend.Models;

namespace RetroBackend.Services;

public interface IItemService
{
    Task<Item> CreateItemAsync(CreateItemRequest request);
    Task<IEnumerable<Item>> GetItemsByColumnAsync(Guid columnId);
    Task<Item?> UpdateItemAsync(Guid id, UpdateItemRequest request);
    Task<ActionItem> CreateActionItemAsync(CreateActionItemRequest request);
    Task<IEnumerable<ActionItem>> GetActionItemsByColumnAsync(Guid columnId);
    Task<ActionItem?> UpdateActionItemAsync(Guid id, UpdateActionItemRequest request);
    Task<ActionItem?> CloseActionItemAsync(Guid id, string closedBy);
    Task<IEnumerable<Item>?> MergeItemsAsync(Guid itemId, Guid targetItemId);
    Task<Item?> UnlinkFromGroupAsync(Guid itemId);
    Task<bool> DeleteAsync(Guid id);
}
