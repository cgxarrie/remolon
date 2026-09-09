---
name: Closed board mutations
overview: Reject remaining item and action-item writes when the retrospective is closed, and align manager action-item update/delete with owner-or-assigned like other manager board actions. Merge already returns 409 on a closed board.
todos:
  - id: closed-guard
    content: Shared helper IsRetrospectiveClosedByItem/Column; reject create/update/delete/unlink/close-item when closed (merge already 409)
    status: pending
  - id: action-manager
    content: Manager update/delete action items if owner OR assigned (not owner-only); optionally restrict CloseActionItem to assignee plus managers
    status: pending
  - id: tests
    content: Closed board returns 409/403; assigned manager can update action items; outsider still 403
    status: pending
isProject: false
---

# Closed-board mutations and action-item authz

## Problem

[ItemsController.MergeItems](RetroBackend/Controllers/ItemsController.cs) already returns **409** when `IsRetrospectiveClosedByItemAsync` is true. Create, update, delete, unlink, and [ActionItemsController](RetroBackend/Controllers/ActionItemsController.cs) still authorize assignment/ownership and **do not** check `Retrospective.IsClosed`. [RetrospectiveService.UpdateAsync](RetroBackend/Services/RetrospectiveService.cs) already no-ops closed retros.

Manager **update/delete** of action items uses only `IsRetrospectiveOwnerByItemAsync`, so an assigned Manager who did not create the retro cannot edit action items. [CloseActionItem](RetroBackend/Controllers/ActionItemsController.cs) allows any assigned user to complete any action item.

## Impact

Closed iterations can still be edited via item APIs (integrity). Action-item rules are inconsistent with create (owner **or** assigned). Completing others’ action items may be intended for the board UI — confirm, then encode it explicitly.

## Approach

- Reuse [IsRetrospectiveClosedByItemAsync](RetroBackend/Services/IRetroAuthorizationService.cs); add `ByColumnAsync` if create-by-column needs it.
- On remaining item and action-item **mutating** endpoints, if closed → `409 Conflict` with a stable message (do not mutate). Include unlink.
- Manager action-item update/delete: `IsRetrospectiveOwnerByItemAsync || IsAssignedToRetrospectiveByItemAsync` (and role Manager).
- **CloseActionItem:** keep assigned-or-owner if the product is “anyone on the board can complete pending”; otherwise restrict to assignee, creator, or Manager. Document the chosen rule in the controller summary.

## Files

- [RetroBackend/Services/RetroAuthorizationService.cs](RetroBackend/Services/RetroAuthorizationService.cs)
- [RetroBackend/Controllers/ItemsController.cs](RetroBackend/Controllers/ItemsController.cs)
- [RetroBackend/Controllers/ActionItemsController.cs](RetroBackend/Controllers/ActionItemsController.cs)
- Frontend disable controls on closed boards if not already ([RetroBoard](RetroFrontend/src/components/RetroBoard.tsx) / detail page)

## Tests / verify

- Closed retro: POST/PUT/DELETE item and action item → 409; GET still 200; merge already 409.
- Assigned manager (not creator) can PUT action item on open board.
- Unassigned user → 403.

## Depends on

`IsRetrospectiveClosedByItemAsync` from the merge fix. Independent of reveal privacy ([09-pre-reveal-privacy.md](09-pre-reveal-privacy.md)).
