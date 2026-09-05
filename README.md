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

The entire stack (frontend, backend, database) runs with a single command.

**Prerequisites:** Docker and Docker Compose installed.

```bash
git clone https://github.com/cgxarrie/ReMolon.git
cd ReMolon
docker compose up --build
```

| Service  | URL                          |
|----------|------------------------------|
| Frontend | http://localhost:3000        |
| Backend  | http://localhost:5145        |
| Swagger  | http://localhost:5145/swagger|
| Mailpit  | http://localhost:8025        |

Register a Manager account from the frontend to create an organization.

---

## Project Structure

```
ReMolon/
├── RetroBackend/       # .NET 10 REST API
├── RetroFrontend/      # React 19 SPA
└── docker-compose.yml  # Full-stack orchestration
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
