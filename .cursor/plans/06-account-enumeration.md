---
name: Account enumeration
overview: Unify error messages and timing on register, user create, assign-by-email, and forgot-password so existence of emails, nicknames, and orgs is not leaked across tenants.
todos:
  - id: assign-create
    content: Generic errors for assign/create when email is missing vs other-org; no global email-exists to Managers of another tenant
    status: pending
  - id: forgot-timing
    content: Constant-time forgot-password path (dummy work when user missing)
    status: pending
  - id: register
    content: Avoid public nickname-exists and org-name-exists oracles where possible; rate-limit with plan 04
    status: pending
isProject: false
---

# Account and email enumeration

## Problem

Distinct responses and timing leak whether an email, nickname, or organization exists:

- [AuthController.Register](RetroBackend/Controllers/AuthController.cs): nickname taken (`400`), org name conflict (`409`).
- [UsersController.Create](RetroBackend/Controllers/UsersController.cs): `"A user with this email already exists."` (global `FindByEmailAsync`).
- [UserAssignmentsController.AssignUser](RetroBackend/Controllers/UserAssignmentsController.cs) / `UnassignUser`: `User not found` vs `"User and retrospective must belong to the same organization."`
- [ForgotPassword](RetroBackend/Controllers/AuthController.cs): same JSON for missing users, but `TrySendPasswordResetEmailAsync` only runs when the user exists (SMTP timing).

Nickname uniqueness is **global** ([RetroDbContext](RetroBackend/Data/RetroDbContext.cs) unique index on `Nickname`), so public register is an oracle for all tenants.

## Impact

Attackers map which emails have accounts, which orgs exist, and can probe invites/assignments across organizations.

## Approach

- **Assign / unassign:** If email is not a user in **this** retro’s organization, return a single message (e.g. cannot assign) and the same status. Do not distinguish missing vs other-org.
- **Create user:** If email exists in another org, do not say “already exists” in a way that confirms a global account. Options: generic `400` “could not create user”, or allow the same email per org only if you change Identity (out of scope unless you drop global unique email — **do not** without a dedicated identity redesign). Safer: generic failure + log internally.
- **Forgot-password:** Always spend similar time (hash dummy / delay) when the user is missing; keep the existing generic success message.
- **Register:** Prefer generic `400` for nickname and org-name collisions on the **public** endpoint, or scope nicknames per organization (index change + migration) so org A cannot probe org B nicknames. Org name uniqueness is product-level; if kept unique, still return a generic conflict to reduce scraping signal, plus rate limit.
- Apply rate limits from [04-jwt-lockout-rate-limit.md](04-jwt-lockout-rate-limit.md) if not already present.

## Files

- [RetroBackend/Controllers/AuthController.cs](RetroBackend/Controllers/AuthController.cs)
- [RetroBackend/Controllers/UsersController.cs](RetroBackend/Controllers/UsersController.cs)
- [RetroBackend/Controllers/UserAssignmentsController.cs](RetroBackend/Controllers/UserAssignmentsController.cs)
- Optional: nickname unique index in [RetroDbContext.cs](RetroBackend/Data/RetroDbContext.cs) + migration (only if scoping nicknames per org)

## Tests / verify

- Assign unknown email vs other-org email: same status and message.
- Forgot-password for random vs existing email: same body; timing within a loose bound (or dummy work invoked in unit test).
- Create user with email that exists in another org: no explicit “exists” string in the response.
- Rate-limit: 429 on burst register/forgot-password.

## Depends on

Rate limiter: reuse [04-jwt-lockout-rate-limit.md](04-jwt-lockout-rate-limit.md) if already implemented; otherwise add a minimal limiter here on auth routes only.
