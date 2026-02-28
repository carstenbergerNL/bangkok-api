# Bangkok – Multi-Platform Application

Bangkok is a **synchronized multi-platform** product: one backend API, one contract, and two client applications that **evolve together**. Each app owns its own code; there is no shared package.

## Repository structure

```
sources/
├── api/              # ASP.NET Core Web API (backend)
├── app/              # Web client (React + Vite)
├── app-native/       # Mobile client (Expo / React Native)
├── sql/              # Database schema and migrations
├── .cursor/rules/     # Cursor rules (including sync-architecture.mdc)
├── package.json      # npm workspaces root
└── README.md         # This file
```

## Architecture overview

- **Backend (api/)**: Single source of truth for business logic, persistence, and REST API. All clients call the same endpoints with the same request/response shapes.
- **Web (app/)**: React SPA with its own models, constants, services, and UI. Platform-specific (axios, localStorage, Vite).
- **Native (app-native/)**: Expo app with its own models, constants, services, and screens. Platform-specific (fetch, SecureStore, Expo Router).

**No shared package**: Each app maintains its own `models/` and `constants/`. They must stay in sync through identical naming, structure, and API contracts.

## Mirror strategy

- **Each app owns its code**: `app/src/models/`, `app/src/constants/`, and `app-native/src/models/`, `app-native/src/constants/` are maintained separately.
- **Identical by design**: Same model names (`Profile`, `CreateProfileRequest`), same API paths (`API_PATHS`), same validation limits (`VALIDATION`), same service method names.
- **When you change one app**: Update the other app’s models/constants/services so they stay aligned.

## Synchronization rules

1. **One feature, two clients**  
   Any user-facing feature must be implemented in both `app` and `app-native` (or explicitly scoped to one platform and documented).

2. **Identical contracts**  
   - Same API endpoints and HTTP methods.  
   - Same request/response shapes; define matching types in both apps.  
   - Same validation rules and limits; keep `VALIDATION` in both `app/src/constants/` and `app-native/src/constants/` identical.

3. **Identical naming**  
   - Same model names (e.g. `Profile`, `CreateProfileRequest`).  
   - Same service/API method names (e.g. `getProfileByUserId`, `login`).  
   - Same folder structure: `api/`, `services/`, `models/`, `constants/`.

4. **Auth flow**  
   - Same logical flow: login → tokens → refresh → logout.  
   - Same token handling semantics; storage mechanism differs (web vs native).

5. **No drift**  
   - When adding a new endpoint or DTO, add it to **both** `app` and `app-native` (models + constants).  
   - When changing a type or constant, update **both** apps.

## Development workflow

1. **Install (from repo root)**  
   ```bash
   npm install
   ```
   This installs dependencies for `app` and `app-native`.

2. **Run web app**  
   ```bash
   npm run dev:app
   ```
   (or `cd app && npm run dev`)

3. **Run native app**  
   ```bash
   npm run dev:app-native
   ```
   (or `cd app-native && npm run dev`)

4. **Backend**  
   Run the ASP.NET Core API from `api/` (e.g. Visual Studio or `dotnet run`). Point both clients at the same API base URL (via env config).

## How to add a new feature properly

1. **Define the contract**  
   - Add or update types in **both** `app/src/models/` and `app-native/src/models/` (keep them identical).  
   - Add or update paths in **both** `app/src/constants/api.ts` and `app-native/src/constants/api.ts`.  
   - Add or update validation in **both** `app/src/constants/validation.ts` and `app-native/src/constants/validation.ts`.

2. **Backend**  
   - Implement or extend the API (controllers, DTOs, services) so request/response match the client types.

3. **Web (app)**  
   - Add or update services in `app/src/services/` using local types and `API_PATHS`.  
   - Add or update UI (pages, components) and route as needed.

4. **Native (app-native)**  
   - Add or update services in `app-native/src/services/` using the same types and `API_PATHS`.  
   - Add or update screens and navigation so the feature is available on mobile with the same semantics.

5. **Avoid divergence**  
   - Do not add a feature only in `app` or only in `app-native` without a documented reason.  
   - Keep models, constants, and API usage identical in both apps.

## How to avoid divergence

- **Use the Cursor rule**  
  See `.cursor/rules/sync-architecture.mdc`. It instructs the IDE to keep web and native in sync.

- **Mirror structure and content**  
  `app/src/models/` and `app-native/src/models/` must define the same types. Same for `constants/`. `app/src/services/` and `app-native/src/services/` should mirror each other (same method names and endpoints).

- **Single contract**  
  API paths and validation limits must be identical in both apps; align backend with them.

## Summary

- **Web and native are two clients of the same system**, not two separate projects.  
- **Each app owns its code**; there is no shared package. Models and constants are duplicated by design and must be kept in sync.  
- Every feature is implemented in both clients with identical contracts, naming, and API usage.
