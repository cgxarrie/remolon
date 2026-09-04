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
| Auth                | POST     | `/api/auth/register`                     | Public           |
| Auth                | POST     | `/api/auth/login`                        | Public           |
| Users               | GET      | `/api/users`                             | Admin            |
| Users               | PUT      | `/api/users/{id}/role`                   | Admin / Manager  |
| Retrospectives      | GET      | `/api/retrospectives`                    | Authenticated    |
| Retrospectives      | GET      | `/api/retrospectives/{id}`               | Authenticated    |
| Retrospectives      | POST     | `/api/retrospectives`                    | Admin / Manager  |
| Retrospectives      | PUT      | `/api/retrospectives/{id}`               | Admin / Manager  |
| Retrospectives      | DELETE   | `/api/retrospectives/{id}`               | Admin / Manager  |
| Retrospectives      | POST     | `/api/retrospectives/{id}/close`         | Admin / Manager  |
| Items               | POST     | `/api/items`                             | Authenticated    |
| Items               | PUT      | `/api/items/{id}`                        | Role-dependent   |
| Items               | DELETE   | `/api/items/{id}`                        | Role-dependent   |
| Action Items        | GET      | `/api/actionitems/{retroId}`             | Authenticated    |
| User Assignments    | POST     | `/api/userassignments`                   | Admin / Manager  |
| User Assignments    | DELETE   | `/api/userassignments`                   | Admin / Manager  |

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
  -e POSTGRES_PASSWORD=retro_password \
  -p 5432:5432 \
  postgres:16-alpine
```

### 2. Configure

Edit `appsettings.Development.json` (or set environment variables):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=retrodb;Username=retro;Password=retro_password"
  },
  "Jwt": {
    "Key": "your-secret-key-at-least-32-chars-long",
    "Issuer": "RetroBackend",
    "Audience": "RetroBackendClients"
  },
  "DefaultAdmin": {
    "Email": "sa@ReMolon.com",
    "Password": "Passw0rd!",
    "Nickname": "sa"
  }
}
```

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
docker compose up backend postgres --build
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
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string            |
| `Jwt:Key`                        | Signing key (min 32 characters)              |
| `Jwt:Issuer`                     | Token issuer                                 |
| `Jwt:Audience`                   | Token audience                               |
| `DefaultAdmin:Email`             | Seed admin email (first run only)            |
| `DefaultAdmin:Password`          | Seed admin password (first run only)         |
| `Cors:AllowedOrigins`            | Array of allowed frontend origins            |
