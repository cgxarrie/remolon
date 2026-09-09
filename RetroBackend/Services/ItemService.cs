using RetroBackend.Models;
using RetroBackend.Repositories;

namespace RetroBackend.Services;

public class ItemService : IItemService
{
    private readonly IItemRepository _repository;
    private readonly IRetroAuthorizationService _authzService;

    public ItemService(IItemRepository repository, IRetroAuthorizationService authzService)
    {
        _repository = repository;
        _authzService = authzService;
    }

    public Task<Item> CreateItemAsync(CreateItemRequest request)
    {
        var item = new Item(request.CreatedBy, request.CreatedByNickname, request.ColumnId, request.Description, request.Position);
        return _repository.AddAsync(item);
    }

    public Task<IEnumerable<Item>> GetItemsByColumnAsync(Guid columnId) =>
        _repository.GetByColumnAsync(columnId);

    public async Task<Item?> UpdateItemAsync(Guid id, UpdateItemRequest request)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null || item is ActionItem) return null;

        if (request.Description is not null)
            item.Description = request.Description;

        if (request.Position is not null)
            item.Position = request.Position.Value;

        return await _repository.UpdateAsync(item);
    }

    public async Task<ActionItem> CreateActionItemAsync(CreateActionItemRequest request)
    {
        var item = new ActionItem(request.CreatedBy, request.CreatedByNickname, request.Assignees, request.ColumnId, request.Description, request.Position);
        return (ActionItem)await _repository.AddAsync(item);
    }

    public Task<IEnumerable<ActionItem>> GetActionItemsByColumnAsync(Guid columnId) =>
        _repository.GetActionItemsByColumnAsync(columnId);

    public async Task<ActionItem?> UpdateActionItemAsync(Guid id, UpdateActionItemRequest request)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is not ActionItem actionItem) return null;

        if (request.Description is not null)
            actionItem.Description = request.Description;

        if (request.Position is not null)
            actionItem.Position = request.Position.Value;

        if (request.Assignees is not null)
            actionItem.Assign(request.Assignees);

        if (request.IsCompleted is true)
            actionItem.Complete(string.Empty);

        await _repository.UpdateAsync(actionItem);
        return actionItem;
    }

    public async Task<ActionItem?> CloseActionItemAsync(Guid id, string closedBy)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is not ActionItem actionItem) return null;

        actionItem.Complete(closedBy);
        return await _repository.UpdateAsync(actionItem) as ActionItem;
    }

    public async Task<IEnumerable<Item>?> MergeItemsAsync(Guid itemId, Guid targetItemId)
    {
        var item = await _repository.GetByIdAsync(itemId);
        var target = await _repository.GetByIdAsync(targetItemId);
        if (item is null || target is null) return null;

        var sourceRetroId = await _authzService.GetRetrospectiveIdByItemAsync(itemId);
        var targetRetroId = await _authzService.GetRetrospectiveIdByItemAsync(targetItemId);
        if (sourceRetroId is null || targetRetroId is null || sourceRetroId != targetRetroId)
            return null;

        // Determine the shared group ID
        var groupId = item.GroupId ?? target.GroupId ?? Guid.NewGuid();

        // If both already belong to different groups, collect all items from the source group
        IEnumerable<Item> sourceGroup = item.GroupId.HasValue && item.GroupId != groupId
            ? await _repository.GetByGroupIdAsync(item.GroupId.Value)
            : [item];

        IEnumerable<Item> targetGroup = target.GroupId.HasValue && target.GroupId != groupId
            ? await _repository.GetByGroupIdAsync(target.GroupId.Value)
            : [target];

        var toUpdate = sourceGroup.Concat(targetGroup)
            .Where(i => i.GroupId != groupId)
            .ToList();

        toUpdate.ForEach(i => i.JoinGroup(groupId));
        await _repository.UpdateRangeAsync(toUpdate);

        return await _repository.GetByGroupIdAsync(groupId);
    }

    public async Task<Item?> UnlinkFromGroupAsync(Guid itemId)
    {
        var item = await _repository.GetByIdAsync(itemId);
        if (item is null) return null;

        var groupId = item.GroupId;
        item.LeaveGroup();
        var updated = await _repository.UpdateAsync(item);
        if (updated is null || groupId is null) return updated;

        var remaining = (await _repository.GetByGroupIdAsync(groupId.Value)).ToList();
        if (remaining.Count == 1)
        {
            remaining[0].LeaveGroup();
            await _repository.UpdateAsync(remaining[0]);
        }

        return updated;
    }

    public Task<bool> DeleteAsync(Guid id) => _repository.DeleteAsync(id);
}
