---
name: Security fix index
overview: Remaining ReMolon security plans after the 2026-09-08 reassessment of fix/security. Cross-tenant list/merge, invite passwords, secrets, SignalR, and payload caps are already on this branch.
todos:
  - id: follow-order
    content: Implement remaining plans in the order below
    status: pending
isProject: false
---

# ReMolon security fix plans

These files are implementation plans only. Pick one file per change set.

## Already done (plans removed)

- **01 Secrets / compose** — JWT and Postgres come from env; placeholder key fails startup; Postgres and Mailpit are not published on the host.
- **02 Participants IDOR** — listing participants requires same org and owner or assignment for all roles.
- **03 Merge target authz** — merge requires both items on the same open retrospective; target is authorized like source.
- **07 Password policy / invites** — Identity requires 12+ chars with complexity; invites use a 30-day set-password link; no temporary password in email or JSON.
- **12 SignalR groups / Throw** — unassigned users are dropped from live groups; throw targets must be on the board.
- **13 Unbounded payloads** — item descriptions capped at 4000 characters; avatar `Cache-Control: private`.

## Still open

1. [08-reset-token-query-string.md](08-reset-token-query-string.md) — Data Protection keys are not persisted (reset 30 min / invite 30 day tokens die on restart and across replicas). URL fragment placement is already done.
2. [04-jwt-lockout-rate-limit.md](04-jwt-lockout-rate-limit.md) — 8h irrevocable JWT; no lockout/rate limits
3. [06-account-enumeration.md](06-account-enumeration.md) — distinct errors/timing on register, create, assign, forgot-password
4. [09-pre-reveal-privacy.md](09-pre-reveal-privacy.md) — author nicknames and pending action items before reveal
5. [10-closed-board-mutations.md](10-closed-board-mutations.md) — item/action writes after close (merge already returns 409)
6. [05-localstorage-xss-headers.md](05-localstorage-xss-headers.md) — token in localStorage; missing CSP/headers
7. [11-tls-docker-hardening.md](11-tls-docker-hardening.md) — TLS, AllowedHosts *, backend published on 5145, migrate-on-startup
8. [14-frontend-role-ui.md](14-frontend-role-ui.md) — ProtectedRoute only checks token presence; persisted `role` still drives Manager UI

## Dependencies

- **04** (refresh/revocation) and **05** (HttpOnly cookies) overlap on how the browser stores credentials. If both are in scope, implement 04’s token model first, then 05’s cookie transport. Rate limits and lockout in 04 do not need cookies. Signing-key rotation from former 01 is already in place.
- **06** can ship without 04, but share the same rate-limit middleware if 04 already added it.
- **08** remaining work is key persistence only. Invite and reset links already use `#email=&token=`. Rotating or losing keys invalidates outstanding 30-day invites.
- **05** cookies need CORS/SameSite decisions from **11** if the API is a different origin than the SPA. Docker nginx same-origin (`/api` proxy) can take cookies without extra CORS credentials.
- **11** compose port changes must not re-publish Postgres/Mailpit (already unpublished in 01). Backend `5145:8080` is still published.
- **09**, **10**, and **14** are independent of token storage. **10** can reuse `IsRetrospectiveClosedByItemAsync` from 03.

## Parallel streams

- **A — Auth:** 04, then 06 (rate limit reuse)
- **B — Email tokens:** 08 key persistence
- **C — Board integrity:** 09, 10
- **D — Deployment / session UI:** 11, then 05 headers/CSP (cookies after 04 if doing both); 14 can ship anytime
