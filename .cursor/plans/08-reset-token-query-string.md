---
name: Reset token query string
overview: Persist ASP.NET Data Protection keys so reset and invite tokens survive restarts and scale-out. Token placement in the URL fragment is already done.
todos:
  - id: fragment
    content: Carry token in URL fragment; parse on ResetPasswordPage
    status: completed
  - id: lifespan
    content: Reset tokens 30 minutes; invite tokens 30 days; Identity one-time use
    status: completed
  - id: dataprotection
    content: Persist Data Protection keys (volume or store) for Docker/multi-instance
    status: pending
isProject: false
---

# Data Protection keys for reset and invite tokens

## Problem

Reset and invite links already use `/reset-password#email=...&token=...` ([PasswordResetEmail.BuildResetUrl](RetroBackend/Services/PasswordResetEmail.cs), [ResetPasswordPage](RetroFrontend/src/pages/ResetPasswordPage.tsx)). [PasswordResetTokenProvider](RetroBackend/Auth/PasswordResetTokenProvider.cs) lasts 30 minutes; [InvitationTokenProvider](RetroBackend/Auth/InvitationTokenProvider.cs) lasts 30 days.

Docker does **not** persist ASP.NET Data Protection keys. Tokens break across backend restarts and do not work across multiple API replicas.

## Impact

A restart invalidates outstanding reset links (30 minutes) and invite links (30 days). Users request many emails. Multi-instance deploys cannot redeem a token issued by another replica.

## Approach

- Persist Data Protection keys: file system volume in compose, or Redis/Azure blob in prod. Document that rotating keys invalidates outstanding reset/invite links.
- Do not log the raw token. Keep Base64url encode/decode as today.
- Do not move tokens back into the query string.

## Files

- [RetroBackend/Program.cs](RetroBackend/Program.cs) (`AddDataProtection().PersistKeysToFileSystem(...)`)
- [docker-compose.yml](docker-compose.yml) key volume (not world-readable)

## Tests / verify

- Restart backend with persisted keys: outstanding reset (30 min) and invite (30 day) tokens still work until expiry.
- Restart without volume (dev check): tokens fail closed.

## Depends on

None. Invite/reset URL format is already aligned.
