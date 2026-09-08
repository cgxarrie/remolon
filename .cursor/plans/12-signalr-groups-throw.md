---
name: SignalR groups and Throw
overview: Stop delivering live events after unassign, and reject Throw unless the target user is a participant on that retrospective.
todos:
  - id: membership
    content: Re-check CanAccess on notify; remove connection from group on unassign; Join one group or leave previous
    status: pending
  - id: throw-target
    content: Validate targetUserId is assigned or owner on that retro before broadcasting ObjectThrown
    status: pending
  - id: tests
    content: Unassigned user does not receive ItemsChanged; Throw to outsider is no-op
    status: pending
isProject: false
---

# SignalR group stickiness and Throw target

## Problem

[RetrospectiveHub.Join](RetroBackend/Hubs/RetrospectiveHub.cs) adds the connection to `retro:{id}` after `CanAccessAsync` but never leaves groups. Unassign does not call `RemoveFromGroup`. [TryCreateThrowAsync](RetroBackend/Services/RetrospectiveRealtimeService.cs) checks caller access and `ThrowableObjects.Allowed` but not that `targetUserId` is on the board.

Live sends go through [RetrospectiveLiveNotifier](RetroBackend/Services/RetrospectiveLiveNotifier.cs) to the group name.

## Impact

A user unassigned from a retro keeps receiving item/reveal/close/delete events until they disconnect. Throw can target arbitrary user ids (client-side harassment / noise).

## Approach

- **Join:** Leave other `retro:*` groups on Join, or document multi-board tabs and still re-validate.
- **Unassign / batch replace:** After saving, notify hub to remove that `userId` from the group (`IHubContext<RetrospectiveHub>` + `IUserIdProvider` so users map to Identity `NameIdentifier`).
- **Notify path:** `CanAccessAsync` before send is expensive; prefer group membership accuracy. Optionally skip notify to users not in `UserRetrospectives`.
- **Throw:** After caller `CanAccessAsync`, require target is assigned or owner of `retrospectiveId` (and not self, already). Invalid target → no broadcast (current `null` return is fine).

## Files

- [RetroBackend/Hubs/RetrospectiveHub.cs](RetroBackend/Hubs/RetrospectiveHub.cs)
- [RetroBackend/Services/RetrospectiveRealtimeService.cs](RetroBackend/Services/RetrospectiveRealtimeService.cs)
- [RetroBackend/Program.cs](RetroBackend/Program.cs) (`AddSignalR`, `IUserIdProvider`)
- [RetroBackend/Controllers/UserAssignmentsController.cs](RetroBackend/Controllers/UserAssignmentsController.cs) (after assign/unassign/batch)
- [RetroFrontend/src/api/retrospectiveHub.ts](RetroFrontend/src/api/retrospectiveHub.ts) only if client must re-Join after kick

## Tests / verify

- Join retro A, unassign via API, mutate board: disconnected client does not get `ItemsChanged` (or connection dropped).
- Throw to a user not on the retro: no `ObjectThrown` to others.
- Throw to another participant: others in group receive event.

## Depends on

None. Hub query-token hardening is [04-jwt-lockout-rate-limit.md](04-jwt-lockout-rate-limit.md).
