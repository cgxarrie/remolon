---
name: Closed board mutations
overview: Reject item and action-item writes when the retrospective is closed, and align manager action-item update/delete with owner-or-assigned like other manager board actions.
todos:
  - id: closed-guard
    content: Shared helper IsRetrospectiveClosedByItem/Column; reject create/update/delete/merge/close-item when closed
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

[ItemsController](RetroBackend/Controllers/ItemsController.cs) and [ActionItemsController](RetroBackend/Controllers/ActionItemsController.cs) authorize assignment/ownership but **do not** check `Retrospective.IsClosed`. [RetrospectiveService.UpdateAsync](RetroBackend/Services/RetrospectiveService.cs) already no-ops closed retros; items do not.

Manager **update/delete** of action items uses only `IsRetrospectiveOwnerByItemAsync`, so an assigned Manager who did not create the retro cannot edit action items. [CloseActionItem](RetroBackend/Controllers/ActionItemsController.cs) allows any assigned user to complete any action item.

## Impact

Closed iterations can still be edited via item APIs (integrity). Action-item rules are inconsistent with create (owner **or** assigned). Completing others’ action items may be intended for the board UI — confirm, then encode it explicitly.

## Approach

- Add `IsRetrospectiveClosedByItemAsync` / `ByColumnAsync` on [IRetroAuthorizationService](RetroBackend/Services/IRetroAuthorizationService.cs).
- On item and action-item **mutating** endpoints, if closed → `409 Conflict` with a stable message (do not mutate).
- Merge/unlink: same closed check (overlaps [03-merge-target-authz.md](03-merge-target-authz.md)).
- Manager action-item update/delete: `IsRetrospectiveOwnerByItemAsync || IsAssignedToRetrospectiveByItemAsync` (and role Manager), matching items merge policy after reveal if you also require reveal for those operations.
- **CloseActionItem:** keep assigned-or-owner if the product is “anyone on the board can complete pending”; otherwise restrict to assignee, creator, or Manager. Document the chosen rule in the controller summary.

## Files

- [RetroBackend/Services/RetroAuthorizationService.cs](RetroBackend/Services/RetroAuthorizationService.cs)
- [RetroBackend/Controllers/ItemsController.cs](RetroBackend/Controllers/ItemsController.cs)
- [RetroBackend/Controllers/ActionItemsController.cs](RetroBackend/Controllers/ActionItemsController.cs)
- Frontend disable controls on closed boards if not already ([RetroBoard](RetroFrontend/src/components/RetroBoard.tsx) / detail page)

## Tests / verify

- Closed retro: POST/PUT/DELETE item and action item → 409; GET still 200.
- Assigned manager (not creator) can PUT action item on open board.
- Unassigned user → 403.

## Depends on

Optional shared helper with [03-merge-target-authz.md](03-merge-target-authz.md). Independent of reveal privacy ([09-pre-reveal-privacy.md](09-pre-reveal-privacy.md)).
