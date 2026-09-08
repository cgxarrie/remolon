---
name: Frontend role and ProtectedRoute
overview: Treat persisted role as display-only; gate UI from /users/me and optional JWT expiry so Standard users cannot rely on localStorage role edits.
todos:
  - id: me-source
    content: Load nickname, role, avatar from GET /users/me after login and on app load; do not trust persisted role for canManage
    status: pending
  - id: protected
    content: ProtectedRoute checks token presence and optionally exp; redirect if /users/me 401
    status: pending
  - id: manager-routes
    content: Users and Organizations pages already Navigate away if not Manager — drive that off server role
    status: pending
isProject: false
---

# Frontend role UI vs persisted JWT fields

## Problem

[ProtectedRoute](RetroFrontend/src/components/ProtectedRoute.tsx) only checks `useAuthStore` `token`. [authStore](RetroFrontend/src/store/authStore.ts) persists `role` from the login JSON, not from a verified server fetch. Users can edit `localStorage` `retro-auth` to `Manager` and see [UsersPage](RetroFrontend/src/pages/UsersPage.tsx) / [OrganizationsPage](RetroFrontend/src/pages/OrganizationsPage.tsx) chrome until APIs return 403. `parseJwt` does not verify the signature (expected for UI, but combined with persisted `role` it is misleading). Expired tokens are not cleared until a 401 interceptor in [client.ts](RetroFrontend/src/api/client.ts).

APIs already enforce `[Authorize(Roles = ...)]`. This plan is UI honesty and fewer confused 403s — not a substitute for [04-jwt-lockout-rate-limit.md](04-jwt-lockout-rate-limit.md) or [05-localstorage-xss-headers.md](05-localstorage-xss-headers.md).

## Approach

- After login and on Layout mount, `GET /api/users/me` ([UsersController.GetMe](RetroBackend/Controllers/UsersController.cs)) and set `role` / nickname / avatar from that DTO.
- `canManage` / nav `visible: role === 'Manager'` in [Layout.tsx](RetroFrontend/src/components/Layout.tsx) use store role **after** `/me` succeeded; show a loading gate so the store cannot flash Manager UI from a tampered persist.
- Optional: decode `exp` without trusting other claims; if expired, `clearAuth` and redirect to login (interceptor already handles 401).
- If 05 removes the token from persist, this still applies to in-memory role.

## Files

- [RetroFrontend/src/components/ProtectedRoute.tsx](RetroFrontend/src/components/ProtectedRoute.tsx)
- [RetroFrontend/src/store/authStore.ts](RetroFrontend/src/store/authStore.ts)
- [RetroFrontend/src/components/Layout.tsx](RetroFrontend/src/components/Layout.tsx)
- [RetroFrontend/src/pages/UsersPage.tsx](RetroFrontend/src/pages/UsersPage.tsx), [OrganizationsPage.tsx](RetroFrontend/src/pages/OrganizationsPage.tsx)
- [RetroFrontend/src/App.tsx](RetroFrontend/src/App.tsx) if a session-bootstrap query wraps routes

## Tests / verify

- Tamper persist `role` to Manager as StandardUser: after reload, Users nav hidden and `/users` redirects once `/me` returns StandardUser; API still 403 if they call it.
- Expired token: redirect to login without rendering the board.
- Manager login: Users/Organizations still available.

## Depends on

None. If [05-localstorage-xss-headers.md](05-localstorage-xss-headers.md) removes persisted token, keep `/me` as the role source.
