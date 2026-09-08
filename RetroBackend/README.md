# RetroBackend

> REST API for ReMolon — built with .NET 10 and ASP.NET Core.

---

## Tech Stack

| Concern        | Library / Tool                              |
|----------------|---------------------------------------------|
| Framework      | ASP.NET Core 10                             |
| ORM            | Entity Framework Core 9 + Npgsql            |
| Auth           | ASP.NET Core Identity + JWT Bearer          |
| Database       | PostgreSQL 16                               |
| API Docs       | Swashbuckle (Swagger / OpenAPI)             |
| Migrations     | EF Core CLI (`dotnet-ef`)                   |

---

## Project Structure

```
RetroBackend/
├── Auth/               # Role constants
├── Config/             # Strongly-typed configuration helpers
├── Controllers/        # HTTP endpoints
├── Data/               # DbContext and design-time factory
├── Dtos/               # Request / response shapes
├── Mappings/           # Entity ↔ DTO extension methods
├── Migrations/         # EF Core migration files
├── Models/             # Domain entities
├── Repositories/       # Data-access interfaces and EF implementations
├── Services/           # Business logic layer
├── Program.cs          # Application entry point and DI wiring
└── appsettings.json    # Configuration
```

---

## API Endpoints

| Resource            | Method   | Route                                    | Auth             |
|---------------------|----------|------------------------------------------|------------------|
| Auth                | POST     | `/api/auth/register`                     | Public (creates org + Manager) |
| Auth                | POST     | `/api/auth/login`                        | Public           |
| Users               | POST     | `/api/users`                             | Manager          |
| Users               | PATCH    | `/api/users/{id}/role`                   | Manager          |
| Retrospectives      | GET      | `/api/retrospectives`                    | Authenticated    |
| Retrospectives      | GET      | `/api/retrospectives/{id}`               | Authenticated    |
| Retrospectives      | POST     | `/api/retrospectives`                    | Manager          |
| Retrospectives      | PUT      | `/api/retrospectives/{id}`               | Manager          |
| Retrospectives      | DELETE   | `/api/retrospectives/{id}`               | Manager          |
| Retrospectives      | POST     | `/api/retrospectives/{id}/close`         | Manager          |
| Items               | POST     | `/api/items`                             | Authenticated    |
| Items               | PUT      | `/api/items/{id}`                        | Role-dependent   |
| Items               | DELETE   | `/api/items/{id}`                        | Role-dependent   |
| Action Items        | GET      | `/api/actionitems/{retroId}`             | Authenticated    |
| User Assignments    | POST     | `/api/userassignments`                   | Manager          |
| User Assignments    | DELETE   | `/api/userassignments`                   | Manager          |

Full interactive docs available at `/swagger` when running.

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

Invitation emails (when a Manager creates a user) are sent via SMTP. Local Compose with `docker-compose.dev.yml` uses [Mailpit](https://github.com/axllent/mailpit) at `http://localhost:8025`. Set `Email__SmtpUser` / `Email__SmtpPassword` for authenticated SMTP.

### 3. Apply migrations

```bash
cd RetroBackend
dotnet ef database update
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
docker compose -f docker-compose.yml -f docker-compose.dev.yml up backend postgres --build
```

---

## Authentication

All protected routes require a `Bearer` token in the `Authorization` header.

```
Authorization: Bearer <token>
```

Obtain a token by calling `POST /api/auth/login` with valid credentials. Tokens carry the user's ID, email, and role as claims.

---

## Configuration Reference

| Key                              | Description                                  |
|----------------------------------|----------------------------------------------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string (from env in Production / Compose) |
| `Jwt:Key` / `Jwt__Key`           | Signing key; required; min 32 characters; must not be the burned placeholder |
| `Jwt:Issuer`                     | Token issuer                                 |
| `Jwt:Audience`                   | Token audience                               |
| `Cors:AllowedOrigins`            | Array of allowed frontend origins            |
| `Email__SmtpUser` / `Email__SmtpPassword` | Optional SMTP credentials          |
