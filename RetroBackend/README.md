# RetroBackend

> REST API and SignalR hub for ReMolon — built with .NET 10 and ASP.NET Core.

---

## Tech Stack

| Concern        | Library / Tool                              |
|----------------|---------------------------------------------|
| Framework      | ASP.NET Core 10                             |
| ORM            | Entity Framework Core 9 + Npgsql            |
| Auth           | ASP.NET Core Identity + JWT in HttpOnly cookies |
| Real-time      | SignalR (`/hubs/retrospective`)             |
| Email          | SMTP via a background worker                |
| Database       | PostgreSQL 16                               |
| API Docs       | Swashbuckle (Swagger / OpenAPI)             |
| Migrations     | EF Core CLI (`dotnet-ef`)                   |

---

## Project Structure

```
RetroBackend/
├── Auth/               # Roles, claims, cookie helpers, token providers
├── Config/             # Strongly-typed configuration helpers
├── Controllers/        # HTTP endpoints
├── Data/               # DbContext and design-time factory
├── Dtos/               # Request / response shapes
├── Hubs/               # Retrospective SignalR hub
├── Mappings/           # Entity ↔ DTO extension methods
├── Migrations/         # EF Core migration files
├── Models/             # Domain entities
├── Repositories/       # Data-access interfaces and EF implementations
├── Services/           # Business logic, email, live notifications
├── Program.cs          # Application entry point and DI wiring
└── appsettings.json    # Configuration
```

---

## API Endpoints

Protected routes authenticate from the `access_token` cookie (or `Authorization: Bearer` if a client sends it). Login, register, refresh, and password endpoints are public except where noted.

| Resource            | Method   | Route                                              | Auth             |
|---------------------|----------|----------------------------------------------------|------------------|
| Auth                | POST     | `/api/auth/register`                               | Public (creates org + Manager) |
| Auth                | POST     | `/api/auth/login`                                  | Public           |
| Auth                | POST     | `/api/auth/logout`                                 | Cookie           |
| Auth                | POST     | `/api/auth/refresh`                                | Refresh cookie   |
| Auth                | POST     | `/api/auth/forgot-password`                        | Public           |
| Auth                | POST     | `/api/auth/reset-password`                         | Public           |
| Auth                | POST     | `/api/auth/change-initial-password`                | Public (invite)  |
| Auth                | POST     | `/api/auth/change-password`                        | Authenticated    |
| Users               | GET      | `/api/users/me`                                    | Authenticated    |
| Users               | PATCH    | `/api/users/me`                                    | Authenticated    |
| Users               | GET      | `/api/users`                                       | Manager          |
| Users               | POST     | `/api/users`                                       | Manager          |
| Users               | PATCH    | `/api/users/{id}/role`                             | Manager          |
| Users               | DELETE   | `/api/users/{id}`                                  | Manager          |
| Avatars             | POST     | `/api/users/me/avatar`                             | Authenticated    |
| Avatars             | DELETE   | `/api/users/me/avatar`                             | Authenticated    |
| Avatars             | GET      | `/api/users/{id}/avatar`                           | Authenticated    |
| Organizations       | GET      | `/api/organizations`                               | Authenticated    |
| Organizations       | GET      | `/api/organizations/{id}`                          | Authenticated    |
| Organizations       | PUT      | `/api/organizations/{id}`                          | Manager          |
| Retrospectives      | GET      | `/api/retrospectives`                              | Authenticated    |
| Retrospectives      | GET      | `/api/retrospectives/sessions`                     | Authenticated    |
| Retrospectives      | GET      | `/api/retrospectives/{id}`                         | Authenticated    |
| Retrospectives      | POST     | `/api/retrospectives`                              | Manager          |
| Retrospectives      | PATCH    | `/api/retrospectives/{id}`                         | Manager          |
| Retrospectives      | DELETE   | `/api/retrospectives/{id}`                         | Manager          |
| Retrospectives      | POST     | `/api/retrospectives/{id}/reveal`                  | Manager          |
| Retrospectives      | POST     | `/api/retrospectives/{id}/close`                   | Manager          |
| Retrospectives      | POST     | `/api/retrospectives/{id}/next-iteration`          | Manager          |
| Items               | POST     | `/api/items`                                       | Authenticated    |
| Items               | PUT      | `/api/items/{id}`                                  | Role-dependent   |
| Items               | DELETE   | `/api/items/{id}`                                  | Role-dependent   |
| Items               | POST     | `/api/items/{id}/merge`                            | Manager          |
| Items               | DELETE   | `/api/items/{id}/group`                            | Role-dependent   |
| Action Items        | POST     | `/api/actionitems`                                 | Authenticated    |
| Action Items        | PUT      | `/api/actionitems/{id}`                            | Role-dependent   |
| Action Items        | POST     | `/api/actionitems/{id}/close`                      | Authenticated    |
| Action Items        | DELETE   | `/api/actionitems/{id}`                            | Role-dependent   |
| User Assignments    | POST     | `/api/user-assignments`                            | Manager          |
| User Assignments    | POST     | `/api/user-assignments/batch`                      | Manager          |
| User Assignments    | DELETE   | `/api/user-assignments`                            | Manager          |
| User Assignments    | GET      | `/api/user-assignments/retrospective/{id}/participants` | Authenticated |
| User Assignments    | GET      | `/api/user-assignments/retrospective/{id}/users`   | Manager          |

JWT and refresh token values are omitted from JSON (`JsonIgnore`) and stored in HttpOnly cookies (`access_token`, `refresh_token`). Access tokens last 15 minutes; refresh tokens last 14 days and rotate on use.

Full interactive docs are at `/swagger` in Development.

### SignalR

Hub URL: `/hubs/retrospective` (cookie auth; query `access_token` is accepted for WebSocket clients that cannot send cookies).

| Client method | Server event |
|---------------|----------------|
| `Join(retrospectiveId)` | — |
| `Throw(retrospectiveId, targetUserId, objectId)` | `ObjectThrown` |
| — | `ItemsChanged`, `RetrospectiveRevealed`, `RetrospectiveClosed`, `RetrospectiveDeleted`, `RetrospectiveAccessRevoked` |

---

## Running Locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 16 (or run via Docker)

### 1. Start the database

```bash
docker run -d \
  --name retrodb \
  -e POSTGRES_DB=retrodb \
  -e POSTGRES_USER=retro \
  -e POSTGRES_PASSWORD="$POSTGRES_PASSWORD" \
  -p 5432:5432 \
  postgres:16-alpine
```

Use a password that is not `retro_password` on any shared or production database. That value was committed historically and is burned.

### 2. Configure

Secrets are not stored in `appsettings.json`. Set environment variables (or user secrets) before `dotnet run`. `appsettings.Development.json` may contain a localhost connection string for local Postgres only.

```bash
export Jwt__Key='a-random-secret-at-least-32-chars'
# Optional if not using appsettings.Development.json:
# export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=retrodb;Username=retro;Password=$POSTGRES_PASSWORD"
```

The process exits on startup if `Jwt:Key` is missing, shorter than 32 characters, or still the committed placeholder `CHANGE_THIS_SECRET_KEY_MIN_32_CHARS_LONG_!!`.

Development stores Data Protection keys in `.dataprotection-keys/` (gitignored) so reset and invitation links survive `dotnet run` restarts. Production and Compose require `DataProtection__KeysDirectory`. Deleting those keys invalidates outstanding links.

Invitation, password-reset, and closed-session action-item emails are sent via SMTP. Local Compose with `docker-compose.dev.yml` uses [Mailpit](https://github.com/axllent/mailpit) at `http://localhost:8025`. For a cdmon mailbox set `Email__SmtpHost` (`smtp.yourdomain.com`), `Email__SmtpPort` (`587`), `Email__EnableSsl` (`true`), `Email__SmtpUser` (full address), `Email__SmtpPassword`, and `Email__From`. Port 465 is not usable because `System.Net.Mail.SmtpClient` only supports STARTTLS. Railway blocks outbound SMTP below the Pro plan.

### 3. Apply migrations

The API no longer migrates on every start. Apply schema first:

```bash
cd RetroBackend
dotnet ef database update
```

Or run the app once with `--migrate` and then start it normally:

```bash
dotnet run -- --migrate
dotnet run
```

### 4. Run

```bash
dotnet run
```

API available at `http://localhost:5145`. Swagger UI at `http://localhost:5145/swagger`.

---

## Running with Docker

From the repository root:

```bash
cp .env.example .env   # set POSTGRES_PASSWORD and JWT_KEY
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```

The `migrate` service applies EF migrations, then `backend` starts. The backend port is not published; use `http://localhost:3000`.

---

## Authentication

The SPA authenticates with HttpOnly cookies (`access_token` on `/`, `refresh_token` on `/api/auth`). Call `POST /api/auth/login` with email and password; the JSON body returns profile fields (email, role, nickname, organization) but not the raw tokens.

`POST /api/auth/refresh` rotates the refresh cookie and issues a new access cookie. `POST /api/auth/logout` clears both.

A `Bearer` token in the `Authorization` header is still accepted for non-browser clients. Tokens carry the user's ID, email, organization, and role as claims.

---

## Configuration Reference

| Key                              | Description                                  |
|----------------------------------|----------------------------------------------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string (from env in Production / Compose) |
| `Jwt:Key` / `Jwt__Key`           | Signing key; required; min 32 characters; must not be the burned placeholder |
| `Jwt:Issuer`                     | Token issuer                                 |
| `Jwt:Audience`                   | Token audience                               |
| `Cors:AllowedOrigins`            | Array of allowed frontend origins            |
| `ASPNETCORE_ALLOWEDHOSTS`        | Semicolon-separated hosts accepted in the `Host` header; replaces the `AllowedHosts` default. Requests from other hosts get `400` |
| `Email__SmtpHost` / `Email__SmtpPort` / `Email__EnableSsl` | SMTP server; Mailpit locally, or cdmon `smtp.yourdomain.com` on 587 with `EnableSsl=true` (STARTTLS) |
| `Email__SmtpUser` / `Email__SmtpPassword` | Optional SMTP credentials (full mailbox address on cdmon) |
| `Email__From` | From address; use the cdmon mailbox |
| `Email__FrontendBaseUrl` | Public frontend origin used in invitation and reset links |
| `Email__TimeoutSeconds` | Per-send SMTP deadline in seconds, default `15`. Used by the background email worker; a firewalled SMTP port would otherwise hang the worker until the process is killed |
| `DataProtection:KeysDirectory` / `DataProtection__KeysDirectory` | Directory for Data Protection keys (reset/invite tokens). Required outside Development. Deleting or rotating keys invalidates outstanding links. |
