# ReMolon

> A full-stack retrospective board application for agile teams.

ReMolon lets teams create and run structured retrospectives. Participants can add items to columns, drag-and-drop to reorder, merge, or move items across columns, and close the session when done. Role-based access control keeps things tidy — Managers own retrospectives, and Standard Users contribute their feedback.

---

## Architecture

```
┌──────────────────────┐        REST / JSON        ┌──────────────────────┐
│   RetroFrontend      │ ◄────────────────────────► │   RetroBackend       │
│  React 19 + Vite     │        JWT Bearer          │  .NET 10 Web API     │
└──────────────────────┘                            └──────────┬───────────┘
                                                               │ EF Core
                                                    ┌──────────▼───────────┐
                                                    │     PostgreSQL 16     │
                                                    └──────────────────────┘
```

| Layer     | Technology                                     |
|-----------|------------------------------------------------|
| Frontend  | React 19, TypeScript, Vite, Tailwind CSS       |
| Backend   | .NET 10, ASP.NET Core, Entity Framework Core   |
| Database  | PostgreSQL 16                                  |
| Auth      | ASP.NET Core Identity + JWT Bearer             |

---

## Quick Start with Docker

The entire stack (frontend, backend, database) runs with Docker Compose.

**Prerequisites:** Docker and Docker Compose installed.

```bash
git clone https://github.com/cgxarrie/ReMolon.git
cd ReMolon
cp .env.example .env
# Set POSTGRES_PASSWORD and JWT_KEY (JWT_KEY must be at least 32 characters)
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```

`docker-compose.dev.yml` publishes Postgres `5432` and Mailpit `1025`/`8025` on the host for local work. Default `docker compose up` does not publish those ports, and does not publish the backend. The API is only reachable through the frontend proxy (`http://localhost:3000/api`). Apply schema with the Compose `migrate` service (`dotnet RetroBackend.dll --migrate`) before the API starts.

| Service  | URL                          |
|----------|------------------------------|
| Frontend | http://localhost:3000        |
| API (via nginx) | http://localhost:3000/api |
| Mailpit (with docker-compose.dev.yml) | http://localhost:8025 |

Local Vite still expects a backend on `http://localhost:5145` (`dotnet run` in RetroBackend). Do not expose `5145` from Compose.

### HTTPS (optional overlay)

Default Compose is HTTP. For a production-like stack with TLS at nginx:

```bash
mkdir -p certs
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout certs/privkey.pem -out certs/fullchain.pem \
  -subj "/CN=localhost"
docker compose -f docker-compose.yml -f docker-compose.tls.yml up --build
```

Then use `https://localhost`. Port 80 redirects to HTTPS. Cookies are `Secure` when nginx sets `X-Forwarded-Proto: https`. If the site is not `localhost`, set `ALLOWED_HOSTS` (maps to `ASPNETCORE_ALLOWEDHOSTS`, semicolon-separated).

Register a Manager account from the frontend to create an organization.

### Deploying behind a platform proxy (e.g. Railway)

The SPA always calls `/api` and `/hubs` on its own origin, so only nginx needs to know where the API lives. On the frontend service set:

```env
BACKEND_UPSTREAM=http://<backend-service>.railway.internal:8080
NGINX_RESOLVER=[fd12::10] ipv6=on valid=1s
```

On the backend service set `ASPNETCORE_ALLOWEDHOSTS` to the frontend's public host (nginx forwards the original `Host`) and `Email__FrontendBaseUrl` to the frontend's public URL. The private network is IPv6-only, so the API must listen on `[::]`.

Railway does not read `docker-compose.yml`, so the `EMAIL_*` variables below have no effect there. Set the `Email__SmtpHost` / `Email__SmtpPort` / `Email__EnableSsl` / `Email__SmtpUser` / `Email__SmtpPassword` / `Email__From` keys directly on the backend service. Note that Hobby, Trial, and Free plans block outbound SMTP, so a cdmon mailbox may still not send from those plans.

### cdmon SMTP (Compose / VPS)

Create a mailbox in cdmon (for example `noreply@yourdomain.com`) and put its SMTP settings in `.env`. Username is the **full email address**, and the password is the plain mailbox password. cdmon has closed port 25, so use **587 with STARTTLS** (`EMAIL_SMTP_ENABLE_SSL=true`). Do not use 465: `System.Net.Mail.SmtpClient` only speaks STARTTLS and cannot open the implicit-TLS connection that 465 requires.

```env
EMAIL_SMTP_HOST=smtp.yourdomain.com
EMAIL_SMTP_PORT=587
EMAIL_SMTP_ENABLE_SSL=true
EMAIL_SMTP_USER=noreply@yourdomain.com
EMAIL_SMTP_PASSWORD=your-mailbox-password
EMAIL_FROM=ReMolon <noreply@yourdomain.com>
```

`EMAIL_SMTP_HOST` is `smtp.` plus your hosting domain (`smtp.cdmon.com` also works). `EMAIL_FROM` should be that same mailbox (or another address the server is allowed to send as). Restart the backend after changing `.env`. Leave these blank to keep catching mail in Mailpit (`http://localhost:8025` with `docker-compose.dev.yml`).

The `EMAIL_*` names above only exist in `docker-compose.yml`, which maps them onto the `Email__*` keys the API reads. Outside Compose — Railway, or any platform that runs `RetroBackend/Dockerfile` directly — set the `Email__*` keys instead, or the API silently falls back to the `appsettings.json` default of `localhost:1025` and every send fails with `Connection refused`:

```env
Email__SmtpHost=smtp.yourdomain.com
Email__SmtpPort=587
Email__EnableSsl=true
Email__SmtpUser=noreply@yourdomain.com
Email__SmtpPassword=your-mailbox-password
Email__From=ReMolon <noreply@yourdomain.com>
```

### Required environment

Copy `.env.example` to `.env` (gitignored) and set:

| Variable | Purpose |
|----------|---------|
| `POSTGRES_PASSWORD` | Postgres password; interpolated into the backend connection string |
| `JWT_KEY` | JWT signing key, at least 32 characters; maps to `Jwt__Key` |
| `EMAIL_SMTP_HOST` / `EMAIL_SMTP_PORT` / `EMAIL_SMTP_ENABLE_SSL` | Compose only; defaults are Mailpit (`mailpit:1025`, SSL off). For cdmon use `smtp.yourdomain.com`, `587`, and `true` |
| `EMAIL_SMTP_USER` / `EMAIL_SMTP_PASSWORD` / `EMAIL_FROM` | Mailbox login and From address; leave empty for Mailpit |
| `BACKEND_UPSTREAM` / `NGINX_RESOLVER` | Optional; nginx proxy target for `/api` and `/hubs` and the DNS server used to resolve it. Compose defaults work as-is; override when the API is not the Compose `backend` service |

Do not commit `.env` or `docker-compose.override.yml`.

The JWT placeholder `CHANGE_THIS_SECRET_KEY_MIN_32_CHARS_LONG_!!` and the password `retro_password` were committed in git history and are **burned**. Do not reuse them on any existing or production deploy. After rotating `JWT_KEY`, existing tokens are invalid and users must log in again.

Password-reset and invitation links are protected with ASP.NET Data Protection. Compose stores those keys in the `dataprotection_keys` volume (`DataProtection__KeysDirectory`). Deleting that volume or replacing the key ring invalidates outstanding reset (30 minutes) and invite (30 days) links. Local `dotnet run` writes keys under `RetroBackend/.dataprotection-keys/` (gitignored).

---

## Project Structure

```
ReMolon/
├── RetroBackend/            # .NET 10 REST API
├── RetroFrontend/           # React 19 SPA
├── docker-compose.yml       # Full-stack orchestration (no DB/Mailpit host ports)
├── docker-compose.dev.yml   # Local Postgres and Mailpit port maps
├── docker-compose.tls.yml   # Optional HTTPS at nginx (certs/ + port 443)
└── .env.example             # Required secrets (copy to .env)
```

- [Backend README](RetroBackend/README.md)
- [Frontend README](RetroFrontend/README.md)

---

## User Roles

| Role         | Capabilities                                                  |
|--------------|---------------------------------------------------------------|
| Manager      | Create, edit, and close retrospectives; manage users and items |
| StandardUser | Participate in assigned retrospectives; manage own items only |

---

## License

[MIT](LICENSE)
Retrospevtive application for teams
