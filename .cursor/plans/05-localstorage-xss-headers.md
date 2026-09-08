---
name: Session storage and security headers
overview: Stop persisting bearer tokens in localStorage, add CSP and related headers on nginx (and Vite dev), and validate column header colors as #RRGGBB.
todos:
  - id: headers
    content: Add CSP, frame-ancestors, nosniff, Referrer-Policy, HSTS (when HTTPS) on nginx; mirror Cache-Control already on Vite where useful
    status: pending
  - id: colors
    content: Validate HeaderColor as #RRGGBB on retrospective column update (same regex as org theme)
    status: pending
  - id: cookies
    content: Move access/refresh to HttpOnly Secure SameSite cookies; drop token from zustand persist; CSRF strategy if cookie auth
    status: pending
isProject: false
---

# localStorage XSS and security headers

## Problem

[RetroFrontend/src/store/authStore.ts](RetroFrontend/src/store/authStore.ts) persists `token` under `retro-auth`. Axios attaches it as `Authorization` ([client.ts](RetroFrontend/src/api/client.ts)). Any future XSS steals the session.

[RetroFrontend/nginx.conf](RetroFrontend/nginx.conf) has no `Content-Security-Policy`, `X-Frame-Options` / `frame-ancestors`, `X-Content-Type-Options`, or `Referrer-Policy`. [vite.config.ts](RetroFrontend/vite.config.ts) only sets cache-busting headers on the dev server.

Column `HeaderColor` allows `[MaxLength(7)]` without hex validation ([UpdateRetrospectiveDtos.cs](RetroBackend/Dtos/UpdateRetrospectiveDtos.cs)); org custom theme already uses `#RRGGBB` in [OrganizationsController.cs](RetroBackend/Controllers/OrganizationsController.cs). Theme CSS variables are applied via `setProperty` in [theme.tsx](RetroFrontend/src/theme.tsx).

## Impact

XSS → full account takeover for up to token lifetime (see [04-jwt-lockout-rate-limit.md](04-jwt-lockout-rate-limit.md)). Weak CSP makes XSS more likely to matter. Invalid color strings are a low-risk injection footgun.

## Approach

**Headers (do even if cookies wait):** In [nginx.conf](RetroFrontend/nginx.conf) add at least:

- `Content-Security-Policy` — `default-src 'self'`; `img-src 'self' data:` (avatars); `connect-src 'self'` (same-origin `/api` and `/hubs`); avoid `'unsafe-inline'` if possible (Tailwind/build may need a nonce or hashes — measure after `npm run build`).
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `X-Frame-Options: DENY` or `Content-Security-Policy: frame-ancestors 'none'`
- HSTS only when TLS is on (coordinate [11-tls-docker-hardening.md](11-tls-docker-hardening.md))

**Colors:** Reuse the org `HexColor` regex on `UpdateRetrospectiveColumnRequest.HeaderColor`.

**Cookies (after 04 token model):** HttpOnly, `Secure` (prod), `SameSite=Lax` or `Strict`. SPA and API same-origin via nginx → cookies work without `AllowCredentials` CORS. If the API is called cross-origin, CORS must `AllowCredentials` and **not** `*`. Drop `token` from persist; keep non-secret profile fields or load from `/users/me`. CSRF: SameSite plus require a custom header (`X-Requested-With`) on mutating cookie-auth requests, or anti-forgery token.

Do not implement TLS termination in this plan ([11-tls-docker-hardening.md](11-tls-docker-hardening.md)).

## Files

- [RetroFrontend/nginx.conf](RetroFrontend/nginx.conf)
- [RetroFrontend/vite.config.ts](RetroFrontend/vite.config.ts) (dev headers)
- [RetroBackend/Dtos/UpdateRetrospectiveDtos.cs](RetroBackend/Dtos/UpdateRetrospectiveDtos.cs) and retrospective update validation
- [RetroFrontend/src/store/authStore.ts](RetroFrontend/src/store/authStore.ts), [client.ts](RetroFrontend/src/api/client.ts), [auth.ts](RetroFrontend/src/api/auth.ts), [retrospectiveHub.ts](RetroFrontend/src/api/retrospectiveHub.ts)
- [RetroBackend/Program.cs](RetroBackend/Program.cs) cookie auth / CORS if needed

## Tests / verify

- After login, Application tab shows no JWT in `localStorage` (`retro-auth`).
- Login/logout and SignalR still work through nginx and Vite proxy.
- `curl -I` frontend: CSP and nosniff present.
- Column color `not-a-color` / `url(x)` rejected; `#4f46e5` accepted.

## Depends on

Cookie auth: [04-jwt-lockout-rate-limit.md](04-jwt-lockout-rate-limit.md) first. HTTPS flags: [11-tls-docker-hardening.md](11-tls-docker-hardening.md). Headers and hex validation do not wait on 04.
