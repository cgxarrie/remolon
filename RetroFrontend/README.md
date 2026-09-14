# RetroFrontend

> React 19 single-page application for ReMolon.

---

## Tech Stack

| Concern        | Library / Tool                              |
|----------------|---------------------------------------------|
| Framework      | React 19 + TypeScript                       |
| Build tool     | Vite 6                                      |
| Styling        | Tailwind CSS 3                              |
| Auth state     | Zustand 5                                   |
| Server state   | TanStack Query (React Query) 5              |
| HTTP           | Axios (`withCredentials`, relative `/api`)  |
| Real-time      | SignalR (`@microsoft/signalr`)              |
| Routing        | React Router v7                             |
| Drag & Drop    | dnd-kit                                     |
| Serving (prod) | nginx (Docker)                              |

---

## Project Structure

```
RetroFrontend/
├── src/
│   ├── api/                # Axios clients and SignalR hub
│   ├── components/         # App shell, board, modals, profile forms
│   ├── pages/              # Route-level screens
│   ├── query/              # Shared TanStack Query invalidation
│   ├── store/              # Zustand auth store
│   ├── types/              # TypeScript DTOs
│   ├── theme.tsx           # Organization theme presets and provider
│   ├── App.tsx             # Route definitions
│   └── main.tsx            # Entry point
├── index.html
├── vite.config.ts          # Dev server proxies /api and /hubs
├── tailwind.config.js
└── Dockerfile
```

---

## Pages

| Page                    | Route                        | Access              |
|-------------------------|------------------------------|---------------------|
| Login                   | `/login`                     | Public              |
| Register                | `/register`                  | Public              |
| Forgot password         | `/forgot-password`           | Public              |
| Reset / invite password | `/reset-password`            | Public (email token)|
| Retrospectives list     | `/retrospectives`            | Authenticated       |
| Retrospective detail    | `/retrospectives/:id`        | Authenticated       |
| Profile                 | `/profile`                   | Authenticated       |
| User guide              | `/help`                      | Authenticated       |
| Users management        | `/users`                     | Manager             |
| Organization settings   | `/organizations`             | Manager             |

`/` redirects to `/retrospectives`. The hamburger menu links to Boards, User guide, and (Managers) Users and Organization. Profile is opened from the header avatar.

---

## Features

- **Cookie sessions** — login sets HttpOnly cookies; Axios retries once after `POST /api/auth/refresh` on 401. There is no `VITE_API_URL`; the SPA always calls `/api` and `/hubs` on its own origin.
- **Role-aware UI** — Manager vs StandardUser controls (create boards, participants, reveal/close, user admin, org theme).
- **Boards and sessions** — retrospectives are grouped by title; each dated run is a session that can be Open or Closed.
- **Pre-reveal privacy** — other authors’ items stay hidden until a Manager reveals the board.
- **Drag & drop** — reorder items, move them between columns, merge after reveal (Managers).
- **Action items** — after reveal, capture assignees and completions; pending items can carry into the next iteration.
- **Lifecycle** — reveal, close (read-only), start next iteration with carried-over work.
- **Organization theming** — Managers pick a preset or custom colors applied across the SPA.
- **Profile** — nickname, password, avatar upload.
- **Live board** — SignalR hub at `/hubs/retrospective` pushes item changes, reveal/close, and throwable objects between participants.
- **User management** — Managers invite Standard Users by email and change roles.

The in-app **User guide** (`/help`) describes these workflows for people using the product.

---

## Running Locally

### Prerequisites

- Node.js 20+ and npm
- Backend listening on `http://localhost:5145` (`dotnet run` in RetroBackend)

### 1. Install dependencies

```bash
cd RetroFrontend
npm install
```

### 2. Start the dev server

```bash
npm run dev
```

App available at `http://localhost:5173`. Vite proxies `/api` and `/hubs` (WebSocket) to `http://localhost:5145`. No `.env` API URL is required.

---

## Running with Docker

From the repository root:

```bash
cp .env.example .env   # set POSTGRES_PASSWORD and JWT_KEY
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```

App available at `http://localhost:3000`.

nginx proxies `/api` and `/hubs` to `BACKEND_UPSTREAM`, resolving that name on every request through `NGINX_RESOLVER`.

| Variable | Compose default | Railway |
|----------|-----------------|---------|
| `BACKEND_UPSTREAM` | `http://backend:8080` | `http://<backend-service>.railway.internal:8080` |
| `NGINX_RESOLVER` | `127.0.0.11 ipv6=off valid=10s` | `[fd12::10] ipv6=on valid=1s` |

`BACKEND_UPSTREAM` must be a full URL. The `http://` prefix is required — nginx fails the request with `invalid URL prefix` without it — and the value carries no trailing slash and no path. Per-request resolution is required on Railway, where the private IP changes on every backend redeploy and the name does not resolve while nginx is starting.

---

## Available Scripts

| Command          | Description                          |
|------------------|--------------------------------------|
| `npm run dev`    | Start Vite dev server with HMR       |
| `npm run build`  | Type-check and build for production  |
| `npm run preview`| Preview the production build locally |
