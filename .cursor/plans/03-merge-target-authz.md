---
name: Merge target authorization
overview: Authorize the merge target item the same way as the source, and require both items to belong to the same non-closed retrospective.
todos:
  - id: same-retro
    content: Reject merge unless source and target share retrospective (and org); authorize target like source
    status: pending
  - id: closed
    content: Reject merge when the retrospective is closed (align with 10 if that ships later)
    status: pending
  - id: tests
    content: Tests for cross-retro merge, unauthorized target, happy path same column/group
    status: pending
isProject: false
---

# Merge target authorization

## Problem

[`MergeItems`](RetroBackend/Controllers/ItemsController.cs) calls `CanMergeItemsAsync` only for the **source** `id`. [`ItemService.MergeItemsAsync`](RetroBackend/Services/ItemService.cs) loads both items by GUID and joins groups with no retrospective check:

```77:84:RetroBackend/Services/ItemService.cs
    public async Task<IEnumerable<Item>?> MergeItemsAsync(Guid itemId, Guid targetItemId)
    {
        var item = await _repository.GetByIdAsync(itemId);
        var target = await _repository.GetByIdAsync(targetItemId);
        if (item is null || target is null) return null;
        var groupId = item.GroupId ?? target.GroupId ?? Guid.NewGuid();
```

`CanMergeItemsAsync` requires revealed + Manager + owner-or-assigned on the source item only.

## Impact

A manager who can merge on retro A and who obtains an item GUID from retro B (or another org) can attach foreign items to a group. Live `ItemsChanged` and GET retrospective then leak or mix content.

## Approach

- Resolve `retrospectiveId` for source and target via [`IRetroAuthorizationService.GetRetrospectiveIdByItemAsync`](RetroBackend/Services/RetroAuthorizationService.cs).
- If either is null → 404. If they differ → 400 (or 404). Optionally confirm both columns’ retros share `OrganizationId` with the caller.
- Run `CanMergeItemsAsync` for **both** item ids (or one check plus “target in same retro”).
- Reject if the retro is closed (same rule as [10-closed-board-mutations.md](10-closed-board-mutations.md); implement a small `IsClosed` lookup here even if 10 is not done).
- Keep self-merge `400` as today.

## Files

- [RetroBackend/Controllers/ItemsController.cs](RetroBackend/Controllers/ItemsController.cs)
- [RetroBackend/Services/ItemService.cs](RetroBackend/Services/ItemService.cs) if you prefer the invariant in the service (same-column/same-retro) in addition to the controller

## Tests / verify

- Merge two items on the same revealed board as assigned manager → 200.
- Target item on a different retro → 400/404; source group unchanged.
- Standard user → 403 even if they own both items (current merge policy).

## Depends on

None. Optional later alignment with [10-closed-board-mutations.md](10-closed-board-mutations.md) for a shared “writes allowed” helper.
