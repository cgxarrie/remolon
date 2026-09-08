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

`docker-compose.dev.yml` publishes Postgres `5432` and Mailpit `1025`/`8025` on the host for local work. Default `docker compose up` does not publish those ports.

| Service  | URL                          |
|----------|------------------------------|
| Frontend | http://localhost:3000        |
| Backend  | http://localhost:5145        |
| Swagger  | http://localhost:5145/swagger|
| Mailpit  | http://localhost:8025        |

Register a Manager account from the frontend to create an organization.

### Required environment

Copy `.env.example` to `.env` (gitignored) and set:

| Variable | Purpose |
|----------|---------|
| `POSTGRES_PASSWORD` | Postgres password; interpolated into the backend connection string |
| `JWT_KEY` | JWT signing key, at least 32 characters; maps to `Jwt__Key` |
| `EMAIL_SMTP_USER` / `EMAIL_SMTP_PASSWORD` | Optional; leave empty for Mailpit |

Do not commit `.env` or `docker-compose.override.yml`.

The JWT placeholder `CHANGE_THIS_SECRET_KEY_MIN_32_CHARS_LONG_!!` and the password `retro_password` were committed in git history and are **burned**. Do not reuse them on any existing or production deploy. After rotating `JWT_KEY`, existing tokens are invalid and users must log in again.

---

## Project Structure

```
ReMolon/
├── RetroBackend/            # .NET 10 REST API
├── RetroFrontend/           # React 19 SPA
├── docker-compose.yml       # Full-stack orchestration (no DB/Mailpit host ports)
├── docker-compose.dev.yml   # Local Postgres and Mailpit port maps
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
