---
name: Security fix index
overview: Remaining ReMolon security work after the 2026-09-09 reassessment of fix/security. The large hardening items on this branch are done; leftover issues are medium, not new cross-tenant bypasses.
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
- **04 JWT / lockout / rate limits** — 15-minute access JWT, hashed rotating refresh tokens, Identity lockout, auth IP rate limits, `ClockSkew = 0`, security-stamp invalidation.
- **07 Password policy / invites** — Identity requires 12+ chars with complexity; invites use a 30-day set-password link; no temporary password in email or JSON.
- **08 Reset/invite tokens** — links use `#email=&token=`; Data Protection keys persist in Compose and local Development.
- **09 Pre-reveal privacy** — other authors’ items and pending action items are hidden until reveal; hidden count only.
- **10 Closed-board mutations** — item and action writes return 409 on a closed retrospective; assigned managers can edit action items.
- **12 SignalR groups / Throw** — unassigned users are dropped from live groups; throw targets must be on the board.
- **13 Unbounded payloads** — item descriptions capped at 4000 characters; avatar `Cache-Control: private`.

Headers, `#RRGGBB` column colors, HttpOnly cookies, generic assign/create errors, forgot-password padding, unpublished backend, non-root containers, `--migrate`, `AllowedHosts`, forwarded headers, and `/users/me` as the post-load role source are also on this branch. They are not fully closed; leftovers live in the files below.

## Still open (2026-09-09 review)

No high/critical cross-tenant or authorization bypass was found on `fix/security`. Remaining items are medium.

1. [05-session-transport.md](05-session-transport.md) — tokens still in JSON and in-memory; SignalR still puts the JWT in the query string.
2. [06-register-enumeration.md](06-register-enumeration.md) — register still distinguishes duplicate-email vs password-policy failures.
3. [11-edge-tls.md](11-edge-tls.md) — default Compose is still HTTP; cookies are not `Secure` unless the request is HTTPS.
4. [14-persisted-role.md](14-persisted-role.md) — Manager chrome can flash from persisted `role` before `/users/me` is applied.
5. [15-refresh-reuse.md](15-refresh-reuse.md) — stolen refresh tokens are not treated as compromise on reuse.

## Dependencies

- **05** before treating cookie auth as XSS-complete. Hub cookies need the same-origin nginx proxy (already true in Compose).
- **11** before production cookie `Secure` / HSTS. Do not re-publish Postgres, Mailpit, or backend `5145`.
- **06** is independent of cookies.
- **14** is UI-only; APIs already enforce `[Authorize(Roles = ...)]`.
- **15** is independent of 05 but easier once refresh tokens are cookie-only.

## Parallel streams

- **A — Session:** 05, then 15
- **B — Enumeration:** 06
- **C — Deploy:** 11
- **D — UI:** 14
