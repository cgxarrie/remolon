# RetroFrontend

> React 19 single-page application for ReMolon.

---

## Tech Stack

| Concern        | Library / Tool                              |
|----------------|---------------------------------------------|
| Framework      | React 19 + TypeScript                       |
| Build tool     | Vite 6                                      |
| Styling        | Tailwind CSS 3                              |
| State          | Zustand 5                                   |
| Server state   | TanStack Query (React Query) 5              |
| HTTP           | Axios                                       |
| Routing        | React Router v7                             |
| Drag & Drop    | dnd-kit                                     |
| Serving (prod) | nginx (Docker)                              |

---

## Project Structure

```
RetroFrontend/
├── src/
│   ├── api/                # Axios API client functions
│   ├── components/         # Reusable UI components
│   │   ├── RetroBoard.tsx      # Main board with columns and items
│   │   ├── ColumnView.tsx      # Single column with drag-and-drop
│   │   ├── ItemCard.tsx        # Draggable item card
│   │   ├── ActionColumnView.tsx
│   │   ├── ActionItemCard.tsx
│   │   ├── MergedGroupCard.tsx
│   │   ├── CreateRetroModal.tsx
│   │   ├── EditRetroModal.tsx
│   │   ├── AssignUserModal.tsx
│   │   ├── Layout.tsx          # App shell with navigation
│   │   └── ProtectedRoute.tsx  # Auth guard wrapper
│   ├── pages/
│   │   ├── LoginPage.tsx
│   │   ├── RegisterPage.tsx
│   │   ├── RetrospectivesPage.tsx
│   │   ├── RetrospectiveDetailPage.tsx
│   │   └── UsersPage.tsx
│   ├── store/              # Zustand stores (auth state, etc.)
│   ├── types/              # TypeScript type definitions
│   ├── App.tsx             # Route definitions
│   └── main.tsx            # Entry point
├── index.html
├── vite.config.ts
├── tailwind.config.js
└── Dockerfile
```

---

## Pages

| Page                    | Route                        | Access              |
|-------------------------|------------------------------|---------------------|
| Login                   | `/login`                     | Public              |
| Register                | `/register`                  | Public              |
| Retrospectives list     | `/`                          | Authenticated       |
| Retrospective detail    | `/retrospectives/:id`        | Authenticated       |
| Users management        | `/users`                     | Manager             |

---

## Features

- **Role-aware UI** — controls and actions are shown or hidden based on the logged-in user's role (Manager / StandardUser).
- **Live board** — columns and items rendered in real time with TanStack Query cache invalidation.
- **Drag & drop** — items can be reordered within a column, moved between columns, or merged onto another item using dnd-kit.
- **Retrospective lifecycle** — create, edit, and close retrospectives; closed boards lock all items.
- **User management** — Managers can view users in their organization, invite Standard Users, and change any user's role.

---

## Running Locally

### Prerequisites

- Node.js 20+ and npm

### 1. Install dependencies

```bash
cd RetroFrontend
npm install
```

### 2. Configure the API URL

Create a `.env.local` file:

```env
VITE_API_URL=http://localhost:5145
```

### 3. Start the dev server

```bash
npm run dev
```

App available at `http://localhost:5173`.

---

## Running with Docker

From the repository root:

```bash
docker compose up frontend --build
```

App available at `http://localhost:3000`.

nginx proxies `/api` and `/hubs` to `BACKEND_UPSTREAM`, resolving that name on every request through `NGINX_RESOLVER`.

| Variable | Compose default | Railway |
|----------|-----------------|---------|
| `BACKEND_UPSTREAM` | `http://backend:8080` | `http://<backend-service>.railway.internal:8080` |
| `NGINX_RESOLVER` | `127.0.0.11 ipv6=off valid=10s` | `[fd12::10] ipv6=on valid=1s` |

`BACKEND_UPSTREAM` is scheme, host and port only: no trailing slash and no path. Per-request resolution is required on Railway, where the private IP changes on every backend redeploy and the name does not resolve while nginx is starting.

---

## Available Scripts

| Command          | Description                          |
|------------------|--------------------------------------|
| `npm run dev`    | Start Vite dev server with HMR       |
| `npm run build`  | Type-check and build for production  |
| `npm run preview`| Preview the production build locally |
