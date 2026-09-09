---
name: Cookie-only session and hub auth
overview: Stop returning access and refresh tokens in JSON, drop in-memory bearer use for the SPA, and authenticate SignalR from the HttpOnly cookie instead of a query-string JWT.
todos:
  - id: json
    content: Auth responses return profile fields only; do not put token or refreshToken in the body
    status: pending
  - id: hub
    content: Remove accessTokenFactory; hub uses withCredentials and the access_token cookie
    status: pending
  - id: csrf
    content: If cookie is the only credential, require a custom header on mutating requests or keep SameSite=Lax same-origin only
    status: pending
isProject: false
---

# Cookie-only session and hub auth

## Problem

HttpOnly cookies and dropping JWT from `localStorage` are done ([AuthCookies](RetroBackend/Auth/AuthCookies.cs), [authStore partialize](RetroFrontend/src/store/authStore.ts)). Login/register/refresh still return `AuthTokenResponse` with `token` and `refreshToken`. Axios keeps the access JWT in memory and sends `Authorization`. [retrospectiveHub.ts](RetroFrontend/src/api/retrospectiveHub.ts) sets `accessTokenFactory`, so SignalR negotiate still uses `?access_token=`. [Program.cs](RetroBackend/Program.cs) prefers that query value for `/hubs`.

## Impact

XSS can still steal tokens from the JSON body or Zustand memory. Hub query tokens can appear in nginx/proxy logs.

## Approach

- `TokenOk` sets cookies and returns email/role/nickname only (no secrets).
- SPA: `withCredentials: true`; do not store or attach bearer tokens.
- Hub: `withCredentials: true` only; cookie fallback in `OnMessageReceived` already exists. Keep query tokens only if a non-browser client needs them.
- CSRF: same-origin nginx is enough with `SameSite=Lax` plus CORS without `*`. Optionally require `X-Requested-With` on mutating cookie-auth requests.

## Files

- [RetroBackend/Controllers/AuthController.cs](RetroBackend/Controllers/AuthController.cs), [UsersController.cs](RetroBackend/Controllers/UsersController.cs)
- [RetroBackend/Dtos/AuthDtos.cs](RetroBackend/Dtos/AuthDtos.cs)
- [RetroFrontend/src/api/client.ts](RetroFrontend/src/api/client.ts), [auth.ts](RetroFrontend/src/api/auth.ts), [retrospectiveHub.ts](RetroFrontend/src/api/retrospectiveHub.ts), [store/authStore.ts](RetroFrontend/src/store/authStore.ts)
- [RetroBackend/Program.cs](RetroBackend/Program.cs) hub `OnMessageReceived` order (cookie before query, or query only if no cookie)

## Tests / verify

- After login, response JSON has no `token` / `refreshToken`; Application tab still has HttpOnly cookies.
- Hub connects through Vite proxy and nginx `/hubs/` without `access_token` in the query.
- Logout clears cookies; `/users/me` is 401.

## Depends on

[11-edge-tls.md](11-edge-tls.md) for `Secure` cookies in production. Refresh reuse: [15-refresh-reuse.md](15-refresh-reuse.md).
