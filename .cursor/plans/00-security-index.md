---
name: Security fix index
overview: ReMolon security work on fix/security after the 2026-09-09 reassessment. All listed plans are implemented.
todos:
  - id: follow-order
    content: Remaining security plans are implemented
    status: completed
isProject: false
---

# ReMolon security fix plans

These files were implementation plans only. The leftover issues from the 2026-09-09 review are done; the issue files were removed.

## Done

- **01 Secrets / compose** — JWT and Postgres come from env; placeholder key fails startup; Postgres and Mailpit are not published on the host.
- **02 Participants IDOR** — listing participants requires same org and owner or assignment for all roles.
- **03 Merge target authz** — merge requires both items on the same open retrospective; target is authorized like source.
- **04 JWT / lockout / rate limits** — 15-minute access JWT, hashed rotating refresh tokens, Identity lockout, auth IP rate limits, `ClockSkew = 0`, security-stamp invalidation.
- **05 Cookie-only session / hub** — login JSON omits `token` / `refreshToken`; SPA uses `withCredentials` and `X-Requested-With`; SignalR uses cookies (query JWT only for `/hubs` when no cookie). CSRF: `SameSite=Lax` plus same-origin nginx.
- **06 Register enumeration** — public register returns the same `"Could not create account."` for duplicate email and password-policy failures.
- **07 Password policy / invites** — Identity requires 12+ chars with complexity; invites use a 30-day set-password link; no temporary password in email or JSON.
- **08 Reset/invite tokens** — links use `#email=&token=`; Data Protection keys persist in Compose and local Development.
- **09 Pre-reveal privacy** — other authors’ items and pending action items are hidden until reveal; hidden count only.
- **10 Closed-board mutations** — item and action writes return 409 on a closed retrospective; assigned managers can edit action items.
- **11 Edge TLS** — optional `docker-compose.tls.yml` + `nginx.tls.conf`; HTTP redirects to HTTPS; HSTS; `X-Forwarded-Proto: https`.
- **12 SignalR groups / Throw** — unassigned users are dropped from live groups; throw targets must be on the board.
- **13 Unbounded payloads** — item descriptions capped at 4000 characters; avatar `Cache-Control: private`.
- **14 Persisted role** — `role` is not persisted; `ProtectedRoute` waits until `/users/me` matches store role/userId.
- **15 Refresh reuse** — reuse of a rotated, unexpired refresh token revokes remaining refresh rows for that user; relational providers rotate with `ExecuteUpdate`.

Headers, `#RRGGBB` column colors, unpublished backend, non-root containers, `--migrate`, `AllowedHosts`, and forwarded headers are also on this branch.
