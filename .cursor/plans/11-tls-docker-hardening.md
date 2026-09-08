---
name: TLS and Docker hardening
overview: Serve the stack over TLS at the edge, restrict AllowedHosts, stop publishing the backend and database, run containers as non-root, and move EF migrations off the request process.
todos:
  - id: ports
    content: Do not publish backend 5145 or Postgres/Mailpit on default compose; frontend-only entry
    status: pending
  - id: hosts-tls
    content: Restrict AllowedHosts; ForwardedHeaders + HTTPS redirect/HSTS when TLS exists
    status: pending
  - id: migrate
    content: Migrate via init job or explicit command, not MigrateAsync in Program.cs on every start
    status: pending
  - id: nonroot
    content: Run backend and nginx images as non-root
    status: pending
isProject: false
---

# TLS, Docker, and host hardening

## Problem

- Backend listens HTTP only (`ASPNETCORE_URLS=http://+:8080` in [RetroBackend/Dockerfile](RetroBackend/Dockerfile)). Frontend nginx [listen 80](RetroFrontend/nginx.conf). No `UseHttpsRedirection` / HSTS in [Program.cs](RetroBackend/Program.cs).
- `"AllowedHosts": "*"` in [appsettings.json](RetroBackend/appsettings.json).
- Compose publishes backend `5145:8080` and (until 01) DB/Mailpit. CORS `AllowAnyHeader` + `AllowAnyMethod` with a localhost origin list in [Program.cs](RetroBackend/Program.cs).
- `await db.Database.MigrateAsync()` runs inside the API process at startup.
- Images likely run as root (no `USER` in Dockerfiles).

## Impact

Cleartext credentials on the network, extra attack surface on Postgres and Swagger-capable Kestrel, Host-header ambiguity, migrations as the web identity, container breakout as root.

## Approach

- **Compose:** Only publish the frontend (and TLS port if you add a reverse proxy). Backend stays on the Docker network. Do not re-expose Postgres/Mailpit (see [01-secrets-and-compose-exposure.md](01-secrets-and-compose-exposure.md)).
- **TLS:** Terminate at nginx or a dedicated proxy; set `X-Forwarded-Proto`; in ASP.NET `UseForwardedHeaders` (known proxy) then HTTPS redirect and HSTS in non-Development. Local Vite HTTP remains for dev.
- **AllowedHosts:** explicit hostnames in Production config/env.
- **CORS:** keep explicit origins; if cookies ([05-localstorage-xss-headers.md](05-localstorage-xss-headers.md)) and cross-origin API, add `AllowCredentials`. Same-origin nginx proxy does not need credentialed CORS.
- **Migrations:** `dotnet ef database update` in an init container or CI job; API assumes schema exists. Fail fast if schema missing.
- **USER:** non-root in both Dockerfiles; nginx `nginx` user; bind ports ≥8080 or use existing 80 only if the platform allows it (or frontend on 8080).

Do not put secrets back in compose ([01-secrets-and-compose-exposure.md](01-secrets-and-compose-exposure.md)).

## Files

- [docker-compose.yml](docker-compose.yml)
- [RetroBackend/Dockerfile](RetroBackend/Dockerfile), [RetroFrontend/Dockerfile](RetroFrontend/Dockerfile)
- [RetroFrontend/nginx.conf](RetroFrontend/nginx.conf)
- [RetroBackend/Program.cs](RetroBackend/Program.cs)
- [RetroBackend/appsettings.json](RetroBackend/appsettings.json)
- [README.md](README.md) (how to run migrations)

## Tests / verify

- `docker compose ps`: only intended host ports.
- HTTP to frontend: redirect to HTTPS in the prod-like overlay (if TLS test certs added).
- API starts when DB already migrated; without migrate-on-start, empty DB should fail clearly.
- Container processes are not uid 0 (`docker compose exec backend id`).

## Depends on

Do not undo [01-secrets-and-compose-exposure.md](01-secrets-and-compose-exposure.md). Cookie `Secure` in [05-localstorage-xss-headers.md](05-localstorage-xss-headers.md) needs this TLS story for production.
