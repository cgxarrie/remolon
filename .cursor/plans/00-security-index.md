---
name: Security fix index
overview: Recommended order and dependencies for the fourteen ReMolon security plan files. Implement secrets rotation first; do not implement application code from this index.
todos:
  - id: follow-order
    content: Implement plans 01 then 02–03, then parallel streams A–D as listed below
    status: pending
isProject: false
---

# ReMolon security fix plans

These files are implementation plans only. Pick one file per change set. Cross-links use repo-relative paths under [`.cursor/plans/`](.cursor/plans/).

**Do this first:** [01-secrets-and-compose-exposure.md](01-secrets-and-compose-exposure.md). Treat the committed JWT key and Postgres password as compromised. Do not ship JWT or cookie work while the placeholder key is still valid.

## Recommended order

1. [01-secrets-and-compose-exposure.md](01-secrets-and-compose-exposure.md) — hardcoded JWT/DB secrets; published Postgres/Mailpit; fail-fast on placeholder JWT
2. [02-participants-idor.md](02-participants-idor.md) — `GetRetrospectiveParticipants` skips org/assignment for Managers
3. [03-merge-target-authz.md](03-merge-target-authz.md) — merge does not authorize the target item
4. [04-jwt-lockout-rate-limit.md](04-jwt-lockout-rate-limit.md) — 8h irrevocable JWT; no lockout/rate limits
5. [06-account-enumeration.md](06-account-enumeration.md) — distinct errors/timing on register, create, assign, forgot-password
6. [07-password-policy-invites.md](07-password-policy-invites.md) — weak passwords; temp password in email and JSON
7. [08-reset-token-query-string.md](08-reset-token-query-string.md) — reset token in query; Data Protection keys
8. [09-pre-reveal-privacy.md](09-pre-reveal-privacy.md) — author nicknames and pending action items before reveal
9. [10-closed-board-mutations.md](10-closed-board-mutations.md) — item/action writes after close
10. [05-localstorage-xss-headers.md](05-localstorage-xss-headers.md) — token in localStorage; CSP/headers
11. [11-tls-docker-hardening.md](11-tls-docker-hardening.md) — TLS, AllowedHosts, published ports, migrate-on-startup
12. [12-signalr-groups-throw.md](12-signalr-groups-throw.md) — stale hub groups; Throw target
13. [13-unbounded-payloads.md](13-unbounded-payloads.md) — unbounded descriptions; avatar cache
14. [14-frontend-role-ui.md](14-frontend-role-ui.md) — ProtectedRoute and persisted role

## Dependencies

- **01** has no code dependencies. Complete it before production deploys of 04 or 05 (those still sign/verify with `Jwt:Key`).
- **02** and **03** are independent of each other and of 01. Do them immediately after 01.
- **04** (refresh/revocation) and **05** (HttpOnly cookies) overlap on how the browser stores credentials. If both are in scope, implement 04’s token model first, then 05’s cookie transport. Rate limits and lockout in 04 do not need cookies.
- **06** can ship without 04, but share the same rate-limit middleware if 04 already added it.
- **07** invite links reuse the reset-token design in **08**. Prefer implementing 08’s token placement before or with 07’s one-time invite links.
- **05** cookies need CORS/SameSite decisions from **11** if the API is a different origin than the SPA. Docker nginx same-origin (`/api` proxy) can take cookies without extra CORS credentials.
- **11** compose port changes must not undo **01** (keep Postgres/Mailpit unpublished).
- **09**, **10**, **12**, **13**, **14** are independent of token storage.

## Parallel streams after 01–03

- **A — Auth:** 04, then 06 (rate limit reuse)
- **B — Email:** 08, then 07
- **C — Board integrity:** 09, 10
- **D — Deployment:** 11, then 05 headers/CSP (cookies after 04 if doing both)
