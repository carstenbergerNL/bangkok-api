# Bangkok – API and Web Application

Bangkok is a backend API with a single web client. The API is the source of truth for business logic and persistence; the app consumes it with its own models, constants, and services.

## Repository structure

```
sources/
├── api/              # ASP.NET Core Web API (backend)
├── app/              # Web client (React + Vite)
├── sql/              # Database schema and migrations
├── json/             # API request body examples (Auth, Users, Projects, Tasks, Timelogs, etc.)
├── .cursor/rules/    # Cursor rules
├── package.json      # npm workspaces root
└── README.md         # This file
```

## API and request examples

- **REST API** – Controllers: Auth, Users, Profile, Roles, Permissions, Projects (CSV export, automation rules), Tasks, Comments, Timelogs, Notifications, Billing (usage, plans, Stripe Checkout), **PlatformAdmin** (Super Admin only: dashboard stats, tenant list, tenant usage, suspend/status/upgrade). All authenticated endpoints use `Authorization: Bearer <token>`. **Usage tracking** per tenant (projects, members, storage, time logs) is stored in `TenantUsage` and updated on project create/delete, member add, file upload/delete, and time log create/delete; plan limits are enforced before those actions.
- **Request examples** – The **`json/`** folder contains sample request bodies for key endpoints (login, register, create/update project, create/update task, create timelog, etc.). See `json/README.md` for the full endpoint table. Use them for Postman, curl, or as a reference for the frontend.

## Architecture overview

- **Backend (api/)**: Single source of truth for business logic, persistence, and REST API.
- **Web (app/)**: React SPA with its own models, constants, services, and UI (axios, localStorage, Vite).

## Development workflow

1. **Install (from repo root)**  
   ```bash
   npm install
   ```
   This installs dependencies for `app`.

2. **Run web app**  
   ```bash
   npm run dev:app
   ```
   (or `cd app && npm run dev`)

3. **Backend**  
   Run the ASP.NET Core API from `api/` (e.g. Visual Studio or `dotnet run`). Point the app at the API base URL via `VITE_API_BASE_URL` (see `app/.env.example`).

## How to add a new feature

1. **Define the contract**  
   - Add or update types in `app/src/models/` to match the API.  
   - Add or update paths in `app/src/constants/api.ts`.  
   - Add or update validation in `app/src/constants/validation.ts` if needed.

2. **Backend**  
   - Implement or extend the API (controllers, DTOs, services) so request/response match the client types.

3. **Web (app)**  
   - Add or update services in `app/src/services/` using local types and `API_PATHS`.  
   - Add or update UI (pages, components) and routes as needed.

## Summary

- **API + web app**: One backend, one client.  
- **App owns its code**: Models, constants, and services live in `app/` and stay aligned with the API contract.
