---
name: Reset token query string
overview: Stop putting password-reset tokens in query strings that hit servers and logs; persist ASP.NET Data Protection keys so tokens survive restarts and scale-out.
todos:
  - id: fragment
    content: Carry token in URL fragment or POST-only flow; parse on ResetPasswordPage without sending token to the API host as a query on navigation
    status: pending
  - id: lifespan
    content: Keep or shorten TokenLifespan; ensure one-time use remains Identity default
    status: pending
  - id: dataprotection
    content: Persist Data Protection keys (volume or store) for Docker/multi-instance
    status: pending
isProject: false
---

# Password reset token in the query string

## Problem

[PasswordResetEmail.BuildResetUrl](RetroBackend/Services/PasswordResetEmail.cs) builds `/reset-password?email=...&token=...`. [ResetPasswordPage](RetroFrontend/src/pages/ResetPasswordPage.tsx) reads `useSearchParams`. Tokens appear in browser history, access logs, and Referer if the user follows an outbound link.

[PasswordResetTokenProvider](RetroBackend/Auth/PasswordResetTokenProvider.cs) uses ASP.NET Data Protection with a 30-minute lifespan. Docker does not persist keys, so tokens break across restarts and do not work across multiple backend replicas.

## Impact

Anyone who can read logs, history, or Referer can complete a reset if they act before expiry. Broken tokens after restart push users to request many emails (and look like an attack).

## Approach

- Put `token` (and optionally email) in the **URL fragment** (`#email=...&token=...`) so it is not sent to nginx or the API on page load. The SPA reads `window.location.hash` and POSTs JSON to `reset-password` as today.
- Alternatively: email a short opaque id that maps to a server-side token (still fragment-free if the page is `/reset-password/:id` with no secret in the path — then the secret stays server-side only). Prefer fragment unless you want a new table.
- Do not log the raw token. Keep Base64url encode/decode as today.
- Persist Data Protection keys: file system volume in compose, or Redis/Azure blob in prod. Document that rotating keys invalidates outstanding reset/invite links.
- Align invite links from [07-password-policy-invites.md](07-password-policy-invites.md) with the same URL pattern.

## Files

- [RetroBackend/Services/PasswordResetEmail.cs](RetroBackend/Services/PasswordResetEmail.cs)
- [RetroFrontend/src/pages/ResetPasswordPage.tsx](RetroFrontend/src/pages/ResetPasswordPage.tsx)
- [RetroBackend/Program.cs](RetroBackend/Program.cs) (`AddDataProtection().PersistKeysToFileSystem(...)`)
- [docker-compose.yml](docker-compose.yml) key volume (not world-readable)
- [RetroBackend/Services/UserInvitationEmail.cs](RetroBackend/Services/UserInvitationEmail.cs) if 07 uses the same builder

## Tests / verify

- Reset link in Mailpit has `#` not `?token=` (or no secret in query).
- Full flow: request reset → open link → new password → login.
- Restart backend with persisted keys: outstanding token still works until expiry.
- Restart without volume (dev check): tokens fail closed.

## Depends on

Works alone. Coordinate URL format with [07-password-policy-invites.md](07-password-policy-invites.md).
