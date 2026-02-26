# Bangkok Admin App

React frontend for the Bangkok API. Provides login, dashboard, settings, and **admin-only** user management (CRUD, restore deleted users).

## Tech stack

- **React 19** + **TypeScript**
- **Vite 7** (dev server, build)
- **React Router 7** (routing, protected routes)
- **Tailwind CSS 3** (styling)
- **Axios** (HTTP client, JWT in headers)

## Project structure

```
app/
├── public/
├── src/
│   ├── api/           # API client (base URL, 401 handling)
│   ├── assets/
│   ├── components/    # AdminRoute, Modal, PrivateRoute, Toast
│   ├── context/       # AuthContext (user, token, login, logout)
│   ├── hooks/         # useDarkMode
│   ├── layouts/       # MainLayout, Sidebar, Topbar
│   ├── models/        # ApiResponse, User, LoginRequest, etc.
│   ├── pages/         # Login, Dashboard, Settings, AdminSettings
│   ├── services/      # authService, userService
│   ├── utils/         # toast
│   ├── App.tsx
│   ├── main.tsx
│   └── index.css
├── .env.example
├── index.html
├── package.json
├── tailwind.config.js
├── tsconfig.json
└── vite.config.ts
```

## Prerequisites

- **Node.js** (e.g. 18+)
- **Bangkok API** running and reachable (e.g. `https://localhost:56028`)

## Setup

1. **Install dependencies**

   ```bash
   cd app
   npm install
   ```

2. **Configure environment**

   Copy `.env.example` to `.env` and set the API base URL:

   ```bash
   cp .env.example .env
   ```

   Edit `.env`:

   - `VITE_API_BASE_URL` – Base URL of the Bangkok API (no trailing slash).  
     Example: `https://localhost:56028`
   - Optional: `VITE_DEFAULT_APPLICATION_ID` – Default application ID for login if your API requires it.

3. **CORS**

   The API must allow your frontend origin. In the API’s CORS config (e.g. `appsettings.json` → `Cors:AllowedOrigins`), include the dev server origin, e.g. `http://localhost:5173`.

## Scripts

| Command       | Description                    |
|---------------|--------------------------------|
| `npm run dev` | Start dev server (Vite, HMR)   |
| `npm run build` | TypeScript check + production build |
| `npm run preview` | Serve production build locally |
| `npm run lint` | Run ESLint                     |

**Dev server:** by default Vite runs at `http://localhost:5173`. In development, the app uses a proxy to `VITE_API_BASE_URL` (see `vite.config.ts`) so the browser talks to the same origin and avoids CORS during local dev.

## Features

- **Authentication**
  - Login with email and password (JWT).
  - Token and user (including role) stored in `localStorage`; 401 from API triggers logout and redirect to login.

- **Layout**
  - **Topbar:** app title, hamburger (toggle sidebar on desktop, open overlay on mobile), dark/light toggle, user dropdown (logout).
  - **Sidebar:** Dashboard, Settings; **Admin Settings** only for users with role `Admin`.

- **Pages**
  - **Login** – Email + password; redirects to `/` when already logged in.
  - **Dashboard** – Home view (placeholder content).
  - **Settings** – General settings (placeholder).
  - **Admin Settings** (admin only):
    - **Users:** list (email, display name, role, active), **Add user** (register), **Edit** (email, display name, role, active; cannot change own “Active”), **Delete** (soft-delete; cannot delete yourself).
    - **Deleted users:** list of soft-deleted users with **Restore** and “Deleted at” time.

- **Guards**
  - **PrivateRoute** – Redirects unauthenticated users to `/login`.
  - **AdminRoute** – Redirects non-admin users to `/`; used for `/admin-settings`.

## API usage

- All authenticated requests send `Authorization: Bearer <token>`.
- Login: `POST /api/Auth/login` (email, password, optional applicationId).
- User list: `GET /api/Users?pageNumber=1&pageSize=500&includeDeleted=true` (admin only); active and deleted users are split in the UI.
- Create user: `POST /api/Auth/register` (admin creates users via register).
- Update user: `PUT /api/Users/:id`.
- Soft-delete: `DELETE /api/Users/:id`.
- Restore: `PATCH /api/Users/:id/restore`.

## Build for production

```bash
npm run build
```

Output is in `dist/`. Serve with any static host; ensure the host is allowed in the API’s CORS and that `VITE_API_BASE_URL` points to the real API URL used in production.
