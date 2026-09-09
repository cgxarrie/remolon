---
name: Edge TLS
overview: Terminate TLS at nginx (or another proxy) for production-like Compose; keep local Vite HTTP. Ports, non-root, migrate-off-API, AllowedHosts, and forwarded headers are already done.
todos:
  - id: tls
    content: Optional compose overlay with certs; HTTPS listen; HSTS on nginx when TLS is on
    status: pending
  - id: hosts
    content: Document AllowedHosts for real DNS names; localhost-only is correct for current Compose
    status: pending
isProject: false
---

# Edge TLS

## Problem

Default Compose publishes only `3000:8080` HTTP. Backend and frontend run as non-root; migrations are a one-shot `--migrate` job; `AllowedHosts` is `localhost;127.0.0.1`; `UseForwardedHeaders` and API `UseHsts` exist. There is still no TLS terminator, so [AuthCookies](RetroBackend/Auth/AuthCookies.cs) sets `Secure` only when `Request.IsHttps` — false on local HTTP.

## Impact

Session cookies and passwords travel in the clear if this stack is exposed on a network. Acceptable for local Docker; not for internet-facing production.

## Approach

- Add a **prod overlay** (or documented reverse proxy) with certificates; nginx `listen 443 ssl` and `add_header Strict-Transport-Security`.
- Set `X-Forwarded-Proto` (already on `/api` and `/hubs`). Do **not** `UseHttpsRedirection` on Kestrel HTTP-inside-Docker.
- Keep Vite HTTP for `dotnet run` + port 5173.
- Do not publish backend `5145`, Postgres, or Mailpit on the default compose file.

## Files

- [docker-compose.yml](docker-compose.yml) or a new `docker-compose.tls.yml`
- [RetroFrontend/nginx.conf](RetroFrontend/nginx.conf)
- [README.md](README.md)

## Tests / verify

- Overlay: HTTP redirects or only 443 is published; cookies have `Secure`.
- Default `docker compose up` still works on `http://localhost:3000`.

## Depends on

Do not undo unpublished DB/Mailpit. Cookie `Secure` in [05-session-transport.md](05-session-transport.md) needs HTTPS in production.
