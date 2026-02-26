# Bangkok – Multi-Platform Application

Bangkok is a **synchronized multi-platform** product: one backend API, one shared contract, and two client applications that **evolve together**.

## Repository structure

```
sources/
├── api/              # ASP.NET Core Web API (backend)
├── app/              # Web client (React + Vite)
├── app-native/      # Mobile client (Expo / React Native)
├── shared/           # Shared TypeScript: types, constants, validation (used by app + app-native)
├── sql/              # Database schema and migrations
├── .cursor/rules/    # Cursor rules (including sync-architecture.mdc)
├── package.json      # npm workspaces root
└── README.md         # This file
```

## Architecture overview

- **Backend (api/)**: Single source of truth for business logic, persistence, and REST API. All clients call the same endpoints with the same request/response shapes.
- **Shared (shared/)**: Environment-agnostic TypeScript consumed by both `app` and `app-native`:
  - **Models / DTOs**: `ApiResponse`, `User`, `Profile`, `LoginRequest`, `LoginResponse`, `PagedResult`, etc.
  - **Constants**: API paths (`API_PATHS`), validation limits (`VALIDATION`).
  - No DOM, no React, no platform-specific APIs—safe to import from web and native.
- **Web (app/)**: React SPA; uses `@bangkok/shared` for types and constants; platform-specific code (e.g. axios, localStorage, Vite) stays in app.
- **Native (app-native/)**: Expo app; uses `@bangkok/shared` for types and constants; platform-specific code (e.g. fetch, SecureStore, Expo Router) stays in app-native.

## Shared strategy

- **What is shared**: API models (DTOs), response types, API path constants, validation constants, and any pure utility that has no dependency on browser or React Native.
- **What is not shared**: UI components, navigation, API client implementation (axios vs fetch), storage (localStorage vs SecureStore), and any platform-specific logic. These are **mirrored** in structure and naming so both clients stay aligned.

## Synchronization rules

1. **One feature, two clients**  
   Any user-facing feature must be implemented in both `app` and `app-native` (or explicitly scoped to one platform and documented).

2. **Identical contracts**  
   - Same API endpoints and HTTP methods.  
   - Same request/response shapes; use types from `@bangkok/shared`.  
   - Same validation rules and limits (define once in `shared/constants/validation.ts`, use in both clients and align backend with them).

3. **Identical naming**  
   - Same model names (e.g. `Profile`, `CreateProfileRequest`).  
   - Same service/API method names where possible (e.g. `getProfileByUserId`, `login`).  
   - Same folder structure: `api/`, `services/`, `models/` (or `screens/` vs `pages/` with a clear mapping).

4. **Auth flow**  
   - Same logical flow: login → tokens → refresh → logout.  
   - Same token handling semantics; storage mechanism differs (web vs native).

5. **No drift**  
   - When adding a new endpoint or DTO, update `shared` first, then both clients.  
   - When changing a type or constant in `shared`, ensure both clients and the backend stay in sync.

## Development workflow

1. **Install (from repo root)**  
   ```bash
   npm install
   ```
   This installs dependencies for `app`, `app-native`, and `shared` and links the workspace.

2. **Build shared**  
   ```bash
   npm run build:shared
   ```
   Run after changing anything in `shared/` so both apps see the latest types and constants.

3. **Run web app**  
   ```bash
   npm run dev:app
   ```
   (or `cd app && npm run dev`)

4. **Run native app**  
   ```bash
   npm run dev:app-native
   ```
   (or `cd app-native && npm run dev`)

5. **Backend**  
   Run the ASP.NET Core API from `api/` (e.g. Visual Studio or `dotnet run`). Point both clients at the same API base URL (via env config).

## How to add a new feature properly

1. **Define the contract**  
   - If it involves API: add or update types in `shared/src/models/` and paths in `shared/src/constants/api.ts`.  
   - If it involves validation: add or update `shared/src/constants/validation.ts`.  
   - Run `npm run build:shared`.

2. **Backend**  
   - Implement or extend the API (controllers, DTOs, services) so request/response match the shared types.

3. **Web (app)**  
   - Add or update services in `app/src/services/` using shared types and `API_PATHS`.  
   - Add or update UI (pages, components) and route as needed.

4. **Native (app-native)**  
   - Add or update services in `app-native/src/services/` using the same shared types and `API_PATHS`.  
   - Add or update screens and navigation so the feature is available on mobile with the same semantics.

5. **Avoid divergence**  
   - Do not add a feature only in `app` or only in `app-native` without a documented reason (e.g. “web-only admin tool”).  
   - Do not duplicate type definitions: import from `@bangkok/shared`.  
   - If you cannot share code (e.g. a helper that must use DOM or RN APIs), keep the same naming and structure in both clients.

## How to avoid divergence

- **Use the Cursor rule**  
  See `.cursor/rules/sync-architecture.mdc`. It instructs the IDE to keep web and native in sync and to prefer shared code.

- **Prefer shared**  
  New types, DTOs, and constants belong in `shared` unless they are truly platform-specific.

- **Mirror structure**  
  `app/src/services/` and `app-native/src/services/` should mirror each other (same service names and method names). Same idea for API usage and auth flow.

- **Single source of truth**  
  API paths and validation limits live in `shared`; both clients and the backend should align with them.

## Summary

- **Web and native are two clients of the same system**, not two separate projects.  
- **Shared** holds types and constants; **app** and **app-native** implement the same features with platform-appropriate UI and platform-specific APIs.  
- Every feature is defined structurally (shared + API), then implemented in both clients so the product stays consistent and maintainable.
