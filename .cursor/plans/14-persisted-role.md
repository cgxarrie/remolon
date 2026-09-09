---
name: Persisted Manager role flash
overview: ProtectedRoute already waits on GET /users/me, but Manager UI still reads persisted zustand role, so a demoted user can flash Manager chrome for one render.
todos:
  - id: me-gate
    content: Drive Layout and manager pages from meQuery.data.role (or stop persisting role)
    status: pending
isProject: false
---

# Persisted role UI flash

## Problem

[ProtectedRoute](RetroFrontend/src/components/ProtectedRoute.tsx) waits for `/users/me` before rendering children, and APIs still enforce Manager. [authStore](RetroFrontend/src/store/authStore.ts) still persists `role`. [Layout.tsx](RetroFrontend/src/components/Layout.tsx) and [UsersPage](RetroFrontend/src/pages/UsersPage.tsx) read the store. `setProfile({ role })` runs in a `useEffect` after children mount, so a demoted user with leftover `role: 'Manager'` can see Manager nav for one frame (or longer if something reads the store before the effect).

This is UI honesty, not an API bypass.

## Approach

- Do not persist `role` (and prefer not persisting `userId` if unused).
- Or pass `me.role` from ProtectedRoute via context and ignore store role until that value exists.
- Keep `/users/me` as the source of truth.

## Files

- [RetroFrontend/src/store/authStore.ts](RetroFrontend/src/store/authStore.ts)
- [RetroFrontend/src/components/Layout.tsx](RetroFrontend/src/components/Layout.tsx)
- [RetroFrontend/src/pages/UsersPage.tsx](RetroFrontend/src/pages/UsersPage.tsx), [OrganizationsPage.tsx](RetroFrontend/src/pages/OrganizationsPage.tsx)

## Tests / verify

- Persist `role: Manager` as a Standard user; after reload, Users nav stays hidden once `/me` returns StandardUser; no Manager page chrome before that.
- Manager login still sees Users/Organizations.

## Depends on

None.
