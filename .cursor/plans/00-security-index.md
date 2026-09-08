---
name: Security fix index
overview: Remaining ReMolon security plans after the 2026-09-08 reassessment. Secrets, description limits, avatar cache, and SignalR group/throw checks are already on this branch.
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
- **12 SignalR groups / Throw** — unassigned users are dropped from live groups; throw targets must be on the board.
- **13 Unbounded payloads** — item descriptions capped at 4000 characters; avatar `Cache-Control: private`.

## Still open

1. [02-participants-idor.md](02-participants-idor.md) — Managers can list participants of any retrospective GUID
2. [03-merge-target-authz.md](03-merge-target-authz.md) — merge does not authorize the target item
3. [04-jwt-lockout-rate-limit.md](04-jwt-lockout-rate-limit.md) — 8h irrevocable JWT; no lockout/rate limits
4. [06-account-enumeration.md](06-account-enumeration.md) — distinct errors/timing on register, create, assign, forgot-password
5. [07-password-policy-invites.md](07-password-policy-invites.md) — weak passwords; temp password in email and JSON
6. [08-reset-token-query-string.md](08-reset-token-query-string.md) — reset token in query; Data Protection keys
7. [09-pre-reveal-privacy.md](09-pre-reveal-privacy.md) — author nicknames and pending action items before reveal
8. [10-closed-board-mutations.md](10-closed-board-mutations.md) — item/action writes after close
9. [05-localstorage-xss-headers.md](05-localstorage-xss-headers.md) — token in localStorage; missing CSP/headers
10. [11-tls-docker-hardening.md](11-tls-docker-hardening.md) — TLS, AllowedHosts *, backend published on 5145, migrate-on-startup
11. [14-frontend-role-ui.md](14-frontend-role-ui.md) — ProtectedRoute only checks token presence; persisted `role` still drives Manager UI

## Dependencies

- **02** and **03** are independent. Do them first among the remaining work (cross-tenant data).
- **04** (refresh/revocation) and **05** (HttpOnly cookies) overlap on how the browser stores credentials. If both are in scope, implement 04’s token model first, then 05’s cookie transport. Rate limits and lockout in 04 do not need cookies. Signing-key rotation from former 01 is already in place.
- **06** can ship without 04, but share the same rate-limit middleware if 04 already added it.
- **07** invite links reuse the reset-token design in **08**. Prefer implementing 08’s token placement before or with 07’s one-time invite links.
- **05** cookies need CORS/SameSite decisions from **11** if the API is a different origin than the SPA. Docker nginx same-origin (`/api` proxy) can take cookies without extra CORS credentials.
- **11** compose port changes must not re-publish Postgres/Mailpit.
- **09**, **10**, and **14** are independent of token storage.

## Parallel streams after 02–03

- **A — Auth:** 04, then 06 (rate limit reuse)
- **B — Email:** 08, then 07
- **C — Board integrity:** 09, 10
- **D — Deployment / session UI:** 11, then 05 headers/CSP (cookies after 04 if doing both); 14 can ship anytime
