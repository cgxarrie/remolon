---
name: Password policy and invites
overview: Tighten Identity password rules and stop emailing or returning temporary passwords; invite with a one-time set-password link instead.
todos:
  - id: policy
    content: Raise RequiredLength and complexity (or denylist); keep CreateUser temp generator in sync until invites change
    status: pending
  - id: invite-link
    content: Invite via one-time token link; HTML-encode as today; never put temp password in JSON
    status: pending
  - id: create-response
    content: Remove temporaryPassword from CreateUserResponse in production
    status: pending
isProject: false
---

# Password policy and invitations

## Problem

Identity in [Program.cs](RetroBackend/Program.cs) requires length 8 and a digit only (`RequireUppercase` / `RequireNonAlphanumeric` false). [UsersController.Create](RetroBackend/Controllers/UsersController.cs) generates a 12-char alphanumeric password, emails it via [UserInvitationEmail](RetroBackend/Services/UserInvitationEmail.cs), and if SMTP fails returns `temporaryPassword` on `CreateUserResponse`. Login blocks `must_change_password` until [change-initial-password](RetroBackend/Controllers/AuthController.cs).

## Impact

Guessable passwords and credentials in mailbox and Manager API responses. Email and JSON copies of passwords increase leak surface (logs, browser history, support screenshots).

## Approach

- Raise `Password.RequiredLength` (12+) and require at least three of: upper, lower, digit, special — or keep digit+length and add a denylist of common passwords. Apply the same rules to register, reset, change-password, and initial change.
- **Invites:** Do not generate a login password. Create the user with a random unusable password (or `GenerateNewAuthenticatorKey`-style random), set `must_change_password` (or a dedicated invite flag), email a **one-time set-password URL** using the same token provider as reset ([PasswordResetTokenProvider](RetroBackend/Auth/PasswordResetTokenProvider.cs)). Frontend: reuse [ResetPasswordPage](RetroFrontend/src/pages/ResetPasswordPage.tsx) or a dedicated invite route.
- [CreateUserResponse](RetroBackend/Dtos/UserDtos.cs): drop `temporaryPassword`. Return `invitationEmailSent` only. If email fails, return `201`/`502` with “invite not sent, retry send” — never the secret.
- Keep HTML encoding in [UserInvitationEmail](RetroBackend/Services/UserInvitationEmail.cs).

Token **placement** in the URL is [08-reset-token-query-string.md](08-reset-token-query-string.md). Implement 08’s fragment (or POST) design for invite links too; if 08 is not done, still avoid putting the password in the email.

## Files

- [RetroBackend/Program.cs](RetroBackend/Program.cs)
- [RetroBackend/Controllers/UsersController.cs](RetroBackend/Controllers/UsersController.cs)
- [RetroBackend/Dtos/UserDtos.cs](RetroBackend/Dtos/UserDtos.cs)
- [RetroBackend/Services/UserInvitationEmail.cs](RetroBackend/Services/UserInvitationEmail.cs)
- [RetroFrontend/src/pages/UsersPage.tsx](RetroFrontend/src/pages/UsersPage.tsx) if it displayed the temp password
- Auth DTOs/pages if invite shares reset-password

## Tests / verify

- Register/`change-password` reject `Password1` if policy forbids it; accept a strong password.
- Create user: response JSON has no password field; mail body has a link, not a password (Mailpit in dev).
- Invite link sets password and clears must-change; login works.
- SMTP failure: no password in API body.

## Depends on

Prefer [08-reset-token-query-string.md](08-reset-token-query-string.md) for how the token is carried. [01-secrets-and-compose-exposure.md](01-secrets-and-compose-exposure.md) if Mailpit is only on an internal network.
