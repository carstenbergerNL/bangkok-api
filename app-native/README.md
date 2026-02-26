# Bangkok – Native app (Expo)

React Native (Expo) client for Bangkok. It **mirrors the web app** (`app/`) in architecture, API usage, and features, and shares types and constants with it via `@bangkok/shared`.

## Prerequisites

- Node.js 18+
- npm (or pnpm/yarn with workspaces support)
- Expo Go on your device, or an emulator/simulator
- Bangkok API running (see main repo README)

## Setup

1. **From repo root** (recommended):
   ```bash
   npm install
   npm run build:shared
   cd app-native && npx expo start
   ```
   This installs workspace deps and builds the shared package so `@bangkok/shared` resolves.

2. **From app-native only**:
   ```bash
   cd app-native
   npm install
   npx expo start
   ```
   Ensure `shared` is built from root first (`npm run build -w shared` or `cd ../shared && npm run build`).

## Environment

Create `app-native/.env` (or use `eas.json` / Expo env) with:

- `EXPO_PUBLIC_API_BASE_URL` – Base URL of the Bangkok API (e.g. `https://localhost:7001` or your deployed API). Leave empty or use a relative URL if you use a proxy.

Other env vars can be added to mirror the web app (e.g. default application ID) as needed.

## How to run

- **Development**: `npm run dev` or `npx expo start` (from `app-native/`).
- **iOS simulator**: `npx expo run:ios` (requires Xcode).
- **Android emulator**: `npx expo run:android` (requires Android Studio / SDK).
- **Expo Go**: Scan the QR code from `npx expo start` with Expo Go.

## Folder structure (mirrors web app)

```
app-native/src/
├── api/           # API client (fetch + auth headers); uses shared types & API_PATHS
├── services/      # authService, userService, profileService (same names as app)
├── screens/       # Login, Dashboard, Profile, AdminSettings (counterpart to app pages)
├── components/    # Reusable UI; PrivateRoute, AdminRoute, Toast, etc.
├── navigation/    # Stack/Tab navigator and route names
├── hooks/         # useAuth, useDarkMode, etc.
├── models/        # Re-export from @bangkok/shared or local-only types only
├── constants/     # Re-export from @bangkok/shared or app-native–specific only
├── utils/         # Helpers (toast, storage adapters)
└── context/       # AuthContext, etc.
```

- **models/** and **constants/** should prefer `@bangkok/shared`. Add local files only for native-specific types or constants.
- **api/**, **services/**, **screens/** should mirror the web app’s structure and naming so the same features exist in both clients.

## Sync principles

1. **Same API**  
   Use the same endpoints and request/response shapes. Import types from `@bangkok/shared` and use `API_PATHS` from shared constants.

2. **Same features**  
   Every feature in the web app (auth, profile, users, password change, etc.) should have an equivalent in the native app. UI and navigation differ; behavior and data contract do not.

3. **Same validation**  
   Use `VALIDATION` from `@bangkok/shared` for field lengths and rules so both clients validate the same way.

4. **No duplicate types**  
   Do not redefine DTOs or API response types in app-native. Use `@bangkok/shared`. If you need a native-only type, put it in `models/` and document why it’s not shared.

5. **Auth flow**  
   Login, refresh, logout, and token storage (e.g. SecureStore) must implement the same logical flow as the web app; only the storage mechanism is different.

## Building for production

- **Expo (EAS or classic)**:
  ```bash
  npx expo export
  # or
  eas build
  ```
- Ensure `shared` is built before building the native app so `@bangkok/shared` resolves.

## Troubleshooting

- **Module not found: @bangkok/shared**  
  From repo root run `npm run build:shared` and then `npm install`. Ensure you are using the workspace root when running installs.

- **API connection**  
  Check `EXPO_PUBLIC_API_BASE_URL` and that the API allows requests from your device/emulator (CORS, HTTPS, or proxy as needed).

- **Sync with web**  
  When in doubt, compare `app-native/src/services/` and `app/src/services/` for the same method names and endpoints, and ensure both use `@bangkok/shared` for types and constants.
