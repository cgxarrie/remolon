---
name: Refresh token reuse detection
overview: Rotating hashed refresh tokens exist, but a second use of a revoked refresh token does not revoke the rest of the user’s sessions.
todos:
  - id: reuse
    content: Atomic rotate (row lock or conditional RevokedAt); on reuse of a revoked token, revoke all refresh tokens for that user
    status: pending
isProject: false
---

# Refresh token reuse detection

## Problem

[AuthTokenService.RefreshAsync](RetroBackend/Services/AuthTokenService.cs) hashes the presented token, marks `RevokedAt`, and issues a new pair. There is no transaction/row lock, and presenting an already-revoked token just returns null. Concurrent refresh with a stolen token can mint two valid sessions. Classic theft (reuse after the victim refreshed) does not kill the attacker’s new chain.

## Impact

A captured refresh token (JSON body, logs, malware) remains useful until expiry if the attacker refreshes first or in parallel.

## Approach

- Update-where `RevokedAt IS NULL` (or `SELECT … FOR UPDATE`) then insert the new hash in the same transaction.
- If the hash exists but is already revoked and unexpired, call `RevokeAllForUserAsync`.
- Optional family/id column to detect reuse after rotation.

## Files

- [RetroBackend/Services/AuthTokenService.cs](RetroBackend/Services/AuthTokenService.cs)
- [RetroBackend.Tests/Controllers/AuthLockoutAndRefreshTests.cs](RetroBackend.Tests/Controllers/AuthLockoutAndRefreshTests.cs)

## Tests / verify

- Two concurrent refreshes with the same raw token: at most one succeeds; the other 401s.
- After a successful rotate, reusing the old token 401s and remaining refresh rows for that user are revoked.

## Depends on

Works with current hashed refresh table. Stronger if [05-session-transport.md](05-session-transport.md) stops putting refresh tokens in JSON.
