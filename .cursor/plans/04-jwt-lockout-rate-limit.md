---
name: JWT lifetime lockout rate limit
overview: Replace 8-hour irrevocable JWTs with short-lived access tokens plus revocable refresh (or security-stamp checks), enable Identity lockout on login, add rate limits, and tighten ClockSkew and hub token delivery.
todos:
  - id: lockout
    content: Use lockout on login and change-initial-password; increment failed access; return 401 without revealing lockout vs bad password if possible
    status: pending
  - id: rate-limit
    content: Rate-limit login, register, forgot-password, reset-password (IP + email where applicable)
    status: pending
  - id: tokens
    content: Short access JWT; refresh tokens hashed in DB, rotate, revoke on logout/password/role/delete
    status: pending
  - id: clock-hub
    content: Set ClockSkew near zero; avoid logging access_token query except for WebSocket handshake if still required
    status: pending
isProject: false
---

# JWT lifetime, lockout, and rate limits

## Problem

Access tokens expire in 8 hours with a new `jti` but no server-side revocation ([AuthController.GenerateJwtAsync](RetroBackend/Controllers/AuthController.cs), [AuthTokenService](RetroBackend/Services/AuthTokenService.cs)). Role and `organizationId` claims are trusted until expiry. [`Login`](RetroBackend/Controllers/AuthController.cs) uses `FindByEmailAsync` + `CheckPasswordAsync` and never `IsLockedOut` / `AccessFailedAsync`. There is no `AddRateLimiter`. JWT `ClockSkew` is the default (~5 minutes). [Program.cs](RetroBackend/Program.cs) copies `access_token` from the query string for any path under `/hubs`.

Password/role changes and user delete do not invalidate outstanding JWTs.

## Impact

Stolen or leaked tokens work for 8 hours. Demoted or deleted users keep privileges. Password spraying is unlimited. Query-string hub tokens can appear in access logs and Referer.

## Approach

**Lockout (do first, small):** Enable Identity lockout defaults (or explicit `MaxFailedAccessAttempts`). On failed password at login and `change-initial-password`, `AccessFailedAsync`. On success, `ResetAccessFailedCountAsync`. Skip issuing JWT if `IsLockedOutAsync`.

**Rate limiting:** `AddRateLimiter` fixed-window (or sliding) policies on `POST /api/auth/login`, `register`, `forgot-password`, `reset-password`. Partition by IP; optionally by normalized email. `429` with `Retry-After`. Reuse this middleware in [06-account-enumeration.md](06-account-enumeration.md).

**Tokens (larger):** Prefer:
- Access JWT lifetime 10–15 minutes, `ClockSkew = TimeSpan.Zero` (or ≤ 30s).
- Refresh token: cryptographically random, store **hash** + user id + expiry + revoked flag. Rotate on use. Revoke all for user on logout, password change, reset, role change, delete.
- Alternatively, if refresh is deferred: add `Identity.SecurityStamp` (or integer `tokenVersion`) as a JWT claim and reject tokens that do not match the current stamp on every request (`OnTokenValidated`). Bump stamp on password/role/delete.

**Hub:** Keep SignalR `accessTokenFactory` on the client ([retrospectiveHub.ts](RetroFrontend/src/api/retrospectiveHub.ts)). If the server still needs query tokens for WebSockets, restrict `OnMessageReceived` to the hub path and document log redaction. Do not put refresh tokens in query strings.

Cookie transport is [05-localstorage-xss-headers.md](05-localstorage-xss-headers.md). If both ship, implement this token model first, then store refresh in HttpOnly cookie.

After [01-secrets-and-compose-exposure.md](01-secrets-and-compose-exposure.md), signing must use the rotated key.

## Files

- [RetroBackend/Program.cs](RetroBackend/Program.cs)
- [RetroBackend/Controllers/AuthController.cs](RetroBackend/Controllers/AuthController.cs)
- [RetroBackend/Services/AuthTokenService.cs](RetroBackend/Services/AuthTokenService.cs)
- [RetroBackend/Controllers/UsersController.cs](RetroBackend/Controllers/UsersController.cs) (role change / delete → revoke or bump stamp)
- New refresh entity + migration if using refresh tokens
- [RetroFrontend/src/api/client.ts](RetroFrontend/src/api/client.ts) and [RetroFrontend/src/store/authStore.ts](RetroFrontend/src/store/authStore.ts) if refresh is client-visible (avoid if cookie-only)

## Tests / verify

- 5+ failed logins lock the account; further attempts 401; unlock after `DefaultLockoutTimeSpan` or admin path.
- Burst login/forgot-password from one IP → 429.
- After password change, old access token (and refresh) fail.
- After role change, old JWT must not keep the old role (refresh/stamp).
- Hub still connects in Vite proxy and Docker nginx `/hubs/`.

## Depends on

[01-secrets-and-compose-exposure.md](01-secrets-and-compose-exposure.md) before any production deploy of new tokens. Cookie storage: [05-localstorage-xss-headers.md](05-localstorage-xss-headers.md).
