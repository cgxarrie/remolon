---
name: Register email enumeration
overview: Public register still leaks whether an email is taken because duplicate-email returns a generic message while password-policy failures return Identity error codes.
todos:
  - id: generic-register
    content: Same 400 body for DuplicateEmail, DuplicateUserName, and password-policy failures; log details server-side
    status: pending
isProject: false
---

# Register email enumeration

## Problem

Assign, invite-create, nickname/org collisions, and forgot-password padding are already generic. [AuthController.Register](RetroBackend/Controllers/AuthController.cs) still branches:

- Existing email → `DuplicateEmail` → `"Could not create account."`
- New email + weak password → `BadRequest(result.Errors)` (`PasswordTooShort`, etc.)

An unauthenticated caller can probe addresses with a short password.

Global unique nicknames remain a product constraint; public register already uses the generic account message for nickname/org collisions. Do not drop unique email without an identity redesign.

Authenticated [UsersController](RetroBackend/Controllers/UsersController.cs) nickname-taken on `/users/me` is same-tenant UX, not a public oracle.

## Approach

Return one 400 message for every failed `CreateAsync` on public register. Keep `[EnableRateLimiting("auth")]`. Log Identity codes at Debug/Information only.

## Files

- [RetroBackend/Controllers/AuthController.cs](RetroBackend/Controllers/AuthController.cs)
- [RetroBackend.Tests/Controllers/AccountEnumerationTests.cs](RetroBackend.Tests/Controllers/AccountEnumerationTests.cs)

## Tests / verify

- Register existing email with a too-short password vs new email with a too-short password: same status and message (or at least no `DuplicateEmail` vs policy split).
- Valid new account still 200.

## Depends on

None. Rate limit from 04 is already on register.
