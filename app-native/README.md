# Bangkok – Native app (Expo)

React Native (Expo) client for Bangkok. It **mirrors the web app** (`app/`) in architecture, API usage, and features. Each app owns its own code; models and constants are maintained separately and must stay in sync.

## Prerequisites

- Node.js 18+
- npm (or pnpm/yarn with workspaces support)
- Expo Go on your device, or an emulator/simulator
- Bangkok API running (see main repo README)

## Setup

1. **From repo root** (recommended):
   ```bash
   npm install
   cd app-native && npx expo start
   ```

2. **From app-native only**:
   ```bash
   cd app-native
   npm install
   npx expo start
   ```

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
├── api/           # API client (fetch + auth headers)
├── services/      # authService, userService, profileService (same names as app)
├── screens/       # Login, Dashboard, Profile, AdminSettings (counterpart to app pages)
├── components/    # Reusable UI; PrivateRoute, AdminRoute, Toast, etc.
├── navigation/    # Stack/Tab navigator and route names
├── hooks/         # useAuth, useDarkMode, etc.
├── models/        # Own types – keep identical to app/src/models/
├── constants/     # Own constants (API_PATHS, VALIDATION) – keep identical to app
├── utils/         # Helpers (toast, storage adapters)
└── context/       # AuthContext, etc.
```

- **models/** and **constants/** are owned by app-native. Keep them identical to `app/src/models/` and `app/src/constants/`.
- **api/**, **services/**, **screens/** should mirror the web app’s structure and naming so the same features exist in both clients.

## Sync principles

1. **Same API**  
   Use the same endpoints and request/response shapes. Define matching types in `models/` and use `API_PATHS` from `constants/`.

2. **Same features**  
   Every feature in the web app (auth, profile, users, password change, etc.) should have an equivalent in the native app. UI and navigation differ; behavior and data contract do not.

3. **Same validation**  
   Keep `VALIDATION` in `constants/` identical to `app/src/constants/validation.ts`.

4. **Mirror types**  
   When adding or changing a DTO or API type in `app/src/models/`, add or change the same in `app-native/src/models/`. No shared package; each app owns its copy.

5. **Auth flow**  
   Login, refresh, logout, and token storage (e.g. SecureStore) must implement the same logical flow as the web app; only the storage mechanism is different.

## Building for production

- **Expo (EAS or classic)**:
  ```bash
  npx expo export
  # or
  eas build
  ```

## Troubleshooting

- **API connection**  
  Check `EXPO_PUBLIC_API_BASE_URL` and that the API allows requests from your device/emulator (CORS, HTTPS, or proxy as needed).

- **Sync with web**  
  When in doubt, compare `app-native/src/services/` and `app/src/services/` for the same method names and endpoints, and compare `models/` and `constants/` in both apps.
