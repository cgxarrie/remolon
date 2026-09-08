---
name: Participants list IDOR
overview: Stop Managers from listing another organization’s retrospective participants by GUID. Require same-org and owner-or-assigned, matching other assignment endpoints.
todos:
  - id: gate-get-participants
    content: Apply CanManageRetrospectiveAsync (or equivalent) to GetRetrospectiveParticipants; 404 for unknown/other-org
    status: pending
  - id: tests
    content: Add tests for other-org manager, same-org unassigned manager, assigned standard user, assigned manager
    status: pending
isProject: false
---

# Participants list IDOR

## Problem

[`GetRetrospectiveParticipants`](RetroBackend/Controllers/UserAssignmentsController.cs) is authorized for Manager or StandardUser. Standard users must be owner or assigned. **Managers skip those checks** and can load every `UserRetrospective` for any `retrospectiveId`:

```185:193:RetroBackend/Controllers/UserAssignmentsController.cs
        if (!isManager)
        {
            var isOwner = await _authzService.IsRetrospectiveOwnerAsync(currentUserId, retrospectiveId);
            var isAssigned = await _context.UserRetrospectives
                .AnyAsync(ur => ur.UserId == currentUserId && ur.RetrospectiveId == retrospectiveId);

            if (!isOwner && !isAssigned)
                return Forbid();
        }
```

`GetRetrospectiveUsers` already uses `CanManage(organizationId)`. Assign/unassign/batch use `CanManageRetrospectiveAsync` (same org **and** owner or assigned).

## Impact

A Manager in org A who knows (or guesses) a retrospective GUID from org B receives emails, nicknames, roles, org names, and avatar URLs for that board.

## Approach

- Load the retrospective first. If missing, `404`.
- For **all** roles, require access:
  - Same organization as the caller’s `organizationId` claim (and DB org of the retro).
  - Owner **or** assigned (`CanManageRetrospectiveAsync` for Managers; owner-or-assigned for StandardUser — same predicate is fine for both).
- Prefer `404` over `403` when the caller must not learn that a foreign retro exists (optional hardening; `403` is acceptable if consistent with `GetById` on retrospectives).
- Do not rely on the JWT org claim alone without comparing to `retro.OrganizationId`.

## Files

- [RetroBackend/Controllers/UserAssignmentsController.cs](RetroBackend/Controllers/UserAssignmentsController.cs)
- [RetroBackend/Services/IRetroAuthorizationService.cs](RetroBackend/Services/IRetroAuthorizationService.cs) only if you extract a shared `CanAccessRetrospectiveAsync`

## Tests / verify

- Manager org A + retro org B → 403 or 404, empty body (no emails).
- Manager same org, not owner/assigned → 403 or 404.
- Manager owner or assigned → 200 with participants.
- Standard user assigned → 200; unassigned → 403/404.

## Depends on

None. Independent of [01-secrets-and-compose-exposure.md](01-secrets-and-compose-exposure.md).
