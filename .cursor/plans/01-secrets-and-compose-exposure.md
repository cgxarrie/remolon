---
name: Secrets and compose exposure
overview: Stop shipping a known JWT signing key and database password, refuse to start with the placeholder key, and stop publishing Postgres and Mailpit to the host by default.
todos:
  - id: fail-fast-jwt
    content: Fail startup if Jwt:Key is missing, too short, or still the placeholder string
    status: pending
  - id: unpublish-ports
    content: Remove host publishes for Postgres 5432 and Mailpit 1025/8025 from default compose (local override file if needed)
    status: pending
  - id: env-secrets
    content: Move connection string, JWT key, and SMTP credentials to env/secret files not committed; document required vars
    status: pending
  - id: rotate
    content: Document that current JWT key and retro_password are compromised and must be rotated on any existing deploy
    status: pending
isProject: false
---

# Secrets and compose exposure

## Problem

[RetroBackend/appsettings.json](RetroBackend/appsettings.json) and [docker-compose.yml](docker-compose.yml) contain a known JWT key (`CHANGE_THIS_SECRET_KEY_MIN_32_CHARS_LONG_!!`), Postgres user `retro` / password `retro_password`, and compose sets `ASPNETCORE_ENVIRONMENT: Production` with that same key. Postgres `5432` and Mailpit `1025`/`8025` are published to the host. [RetroBackend/appsettings.Development.json](RetroBackend/appsettings.Development.json) repeats the connection string.

JWT is built in [RetroBackend/Controllers/AuthController.cs](RetroBackend/Controllers/AuthController.cs) and [RetroBackend/Services/AuthTokenService.cs](RetroBackend/Services/AuthTokenService.cs) via `_configuration["Jwt:Key"]`. Validation in [RetroBackend/Program.cs](RetroBackend/Program.cs) uses the same key with no check that it was replaced.

## Impact

Anyone with the repo (or a naive `docker compose up` on a reachable machine) can forge JWTs as any user, connect to Postgres, and read password-reset and invite mail from Mailpit.

## Approach

- Keep Development-only example values out of anything used as Production. Prefer `appsettings.json` without secrets; bind `Jwt__Key`, `ConnectionStrings__DefaultConnection`, and `Email__*` from the environment.
- On startup in [RetroBackend/Program.cs](RetroBackend/Program.cs), reject a missing key, key shorter than 32 characters, or exact match to the current placeholder. Exit non-zero before `app.Run()`.
- Default [docker-compose.yml](docker-compose.yml): do not `ports:` Postgres or Mailpit. Add an optional `docker-compose.override.yml` (gitignored) or `docker-compose.dev.yml` for local port mapping and Mailpit UI.
- Require `Jwt__Key` and `POSTGRES_PASSWORD` (interpolated into the connection string) in compose via env file that is not committed; ship `.env.example` with empty placeholders and comments.
- README: list required env vars; state that published credentials in git history are burned.

Do not implement refresh tokens or cookies here (see [04-jwt-lockout-rate-limit.md](04-jwt-lockout-rate-limit.md) and [05-localstorage-xss-headers.md](05-localstorage-xss-headers.md)).

## Files

- [docker-compose.yml](docker-compose.yml)
- [RetroBackend/appsettings.json](RetroBackend/appsettings.json)
- [RetroBackend/appsettings.Development.json](RetroBackend/appsettings.Development.json)
- [RetroBackend/Program.cs](RetroBackend/Program.cs)
- [README.md](README.md) and [RetroBackend/README.md](RetroBackend/README.md)
- New `.env.example`; gitignore `.env` and `docker-compose.override.yml` if used

## Tests / verify

- Start backend with placeholder `Jwt__Key` and confirm process exits before listening.
- Start with a 32+ char random key and confirm login still issues a token.
- `docker compose config` on default compose: no `5432:5432` or Mailpit host ports.
- Confirm `.env` and real secrets are not staged (`git status`).

## Depends on

None. Blocker for production use of 04 and 05.
