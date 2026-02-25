# Bangkok API

A .NET Web API built with **Clean Architecture**, **JWT authentication** (access + refresh tokens), **Dapper** for data access, and **SQL Server**. Designed as an enterprise-ready foundation with health checks, structured logging, and consistent API responses.

---

## Table of contents

- [Features](#features)
- [Tech stack](#tech-stack)
- [Architecture](#architecture)
- [Solution structure](#solution-structure)
- [Database](#database)
- [API overview](#api-overview)
- [Configuration](#configuration)
- [Getting started](#getting-started)
- [Docker / Linux deployment](#docker--linux-deployment)
- [Health endpoints](#health-endpoints)
- [Development](#development)

---

## Features

- **JWT authentication** — Access tokens (short-lived) and refresh tokens (long-lived), with refresh tokens stored in SQL Server
- **User registration and login** — Email/password with hashed passwords and salt
- **Token refresh and revoke** — Exchange refresh token for new tokens; revoke refresh tokens (authenticated)
- **Password recovery** — Forgot-password (generic success, no email enumeration); reset-password with secure recovery string and expiry
- **User profile and list** — Get/update user (safe fields only); users can update own email; Admin can list all (paginated), update Role and IsActive
- **User lock and unlock** — Admin-only: lock user (default 15 min or custom UTC `lockoutEnd`); unlock clears lockout. Login returns 403 while locked.
- **User soft delete and restore** — Admin-only soft delete (IsDeleted, DeletedAt); restore endpoint; all GETs exclude deleted users
- **User hard delete** — Admin-only permanent delete with `?confirm=true`; referential integrity (refresh tokens deleted first); self-delete blocked
- **Brute force protection** — In-memory IP, email, and IP+email tracking. Thresholds: IP 10/5min, email 5/5min, IP+email 5/5min. Exponential IP lock escalation (30min → 2h → 24h) when re-triggered within 24h. 429 with Retry-After; no DB/Redis; single-instance only.
- **Health checks** — Liveness, readiness, and full health with SQL Server check
- **Structured logging** — Serilog with console and file sinks, correlation ID and request ID enrichers
- **Global error handling** — Unhandled exceptions return a consistent `ApiResponse` with correlation ID
- **Swagger/OpenAPI** — In Development, with Bearer JWT support
- **Options pattern** — Strongly typed configuration (JWT, connection strings, Serilog)
- **Docker** — Multi-stage Dockerfile (Linux, port 8080, non-root), docker-compose with API + SQL Server, optional logs volume; suitable for Ubuntu/Debian and VPS
- **API response wrapper** — All responses use `ApiResponse<T>`: `success`, `message`, `data`, `errors` (array), `correlationId`; 400/401/403/404/429/500 use the same shape; no internal exception details in production
- **CORS** — Configurable via appsettings (Cors:AllowedOrigins, AllowedHeaders, AllowedMethods); default allows specific origins only
- **Audit logging** — Serilog-based audit for user created/updated/deleted, role changed, login success/failure, account lockout, IP block; no DB audit tables; no passwords or secrets logged

---

## Tech stack

| Layer / concern | Technology |
|-----------------|------------|
| Runtime | .NET 10 |
| Web framework | ASP.NET Core (minimal hosting) |
| Data access | Dapper (no Entity Framework) |
| Database | SQL Server |
| Authentication | JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer) |
| Logging | Serilog (console + file, enrichers) |
| API docs | Swashbuckle (Swagger) |
| Health checks | ASP.NET Core HealthChecks + AspNetCore.HealthChecks.SqlServer |

---

## Architecture

The solution follows **Clean Architecture** with strict dependency direction:

```
Domain → Application → Infrastructure → Api
```

- **Domain** — Entities only (e.g. `User`, `RefreshToken`). No dependencies.
- **Application** — Interfaces (repositories, services), DTOs, options, and response models. Depends only on Domain.
- **Infrastructure** — Implementations: Dapper repositories, SQL connection factory, JWT service, password hasher, auth service. Implements Application interfaces.
- **Api** — Controllers, middleware, `Program.cs`. Thin controllers that delegate to Application services.

Conventions:

- **Controllers** are thin; no business logic. All logic lives in Application/Infrastructure.
- **Data access** is Dapper-only. All SQL lives in Infrastructure (or could be in `/sql` and loaded as needed).
- **Async** everywhere for I/O and service methods.
- **Responses** use `ApiResponse<T>` and `ErrorResponse` from `Bangkok.Application.Models`.

---

## Solution structure

```
sources/
├── api/
│   ├── Bangkok.sln
│   ├── Dockerfile             # Multi-stage build (SDK → aspnet), port 8080, non-root
│   ├── .dockerignore          # Excludes bin, obj, .git, logs, .env, etc.
│   ├── Bangkok.Api/           # Web API project
│   │   ├── Controllers/       # AuthController, UsersController
│   │   ├── Middleware/        # CorrelationId, RequestIdEnricher, ExceptionHandling
│   │   ├── Services/          # IIpBlockService, IpBlockService (in-memory brute force)
│   │   ├── Program.cs
│   │   ├── appsettings.Example.json
│   │   └── Properties/launchSettings.json
│   ├── Bangkok.Application/   # Interfaces, DTOs, options, response models
│   ├── Bangkok.Domain/        # User, RefreshToken entities
│   └── Bangkok.Infrastructure/# Repositories, AuthService, JwtService, PasswordHasher, DI
├── sql/
│   ├── 001_initial.sql        # User + RefreshToken tables
│   └── alters/               # Schema change scripts (002–006: display name, password recovery, is_active, soft delete, lockout)
├── docker-compose.yml         # bangkok-api + sqlserver (Linux)
├── .env.example               # Example env for compose (copy to .env)
├── json/                      # Example request bodies (login, register, …)
├── .cursor/rules/             # Architecture rules
├── .gitignore
└── README.md
```

---

## Database

- **Server:** SQL Server
- **Conventions:** `UNIQUEIDENTIFIER` for primary keys, `DATETIME2(7)` for dates, `NVARCHAR` for strings.

**Tables:**

- **User** — `Id`, `Email` (unique), `DisplayName`, `PasswordHash`, `PasswordSalt`, `Role`, `IsActive`, `CreatedAtUtc`, `UpdatedAtUtc`, `RecoverString`, `RecoverStringExpiry`, `IsDeleted`, `DeletedAt`, `FailedLoginAttempts`, `LockoutEnd` (account lockout; via alters on existing DBs).
- **RefreshToken** — `Id`, `UserId` (FK), `Token`, `ExpiresAtUtc`, `CreatedAtUtc`, `RevokedReason`, `RevokedAtUtc`.

Apply schema:

1. Create a database (e.g. `BangkokDb`).
2. Run `sql/001_initial.sql`.
3. Run any scripts in `sql/alters/` in order (e.g. `002_add_user_display_name.sql` … `006_add_lockout_columns.sql`).

---

## API overview

Base path: **`/api`**. All success/error responses use the wrapper (`ApiResponse<T>`): `success`, `message` (optional), `data`, `error` (single, backward compatible), `errors` (array), `correlationId`. 400, 401, 403, 404, 429, 500 use the same shape. Internal exception details are not exposed in production.

Success example:

```json
{
  "success": true,
  "message": null,
  "data": { ... },
  "error": null,
  "errors": null,
  "correlationId": "..."
}
```

Error example:

```json
{
  "success": false,
  "message": "Invalid email or password.",
  "data": null,
  "error": { "code": "INVALID_CREDENTIALS", "message": "Invalid email or password.", "details": null },
  "errors": [{ "code": "INVALID_CREDENTIALS", "message": "Invalid email or password." }],
  "correlationId": "..."
}
```

### Auth endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/auth/register` | No | Register user; returns access + refresh tokens |
| `POST` | `/api/auth/login` | No | Login; returns access + refresh tokens. 403 if account locked (5 failed attempts); 429 if IP/email/IP+email brute force block (includes Retry-After header). |
| `POST` | `/api/auth/refresh` | No | Exchange refresh token for new access + refresh tokens |
| `POST` | `/api/auth/revoke` | Bearer | Revoke a refresh token |
| `POST` | `/api/auth/forgot-password` | No | Request password recovery; always returns generic success (no email enumeration) |
| `POST` | `/api/auth/reset-password` | No | Reset password with recovery string from forgot-password |

**Register** — Body: `{ "email": "...", "password": "...", "displayName": "...", "role": "User" }`. Password min length 8. `displayName` optional.

**Login** — Body: `{ "email": "...", "password": "..." }`. Returns 403 with `ACCOUNT_LOCKED` if the account is locked (e.g. 5 failed attempts). Returns 429 with `TOO_MANY_ATTEMPTS` ("Too many failed attempts. Try again later.") and a **Retry-After** header (seconds) when blocked by brute force: IP (10 failures in 5 min), email (5 in 5 min), or same IP+email (5 in 5 min). IP blocks use exponential escalation: first 30 min, repeat within 24h → 2h, then 24h; escalation resets after 24h with no block.

**Refresh** — Body: `{ "refreshToken": "..." }`.

**Revoke** — Body: `{ "refreshToken": "..." }`. Header: `Authorization: Bearer <accessToken>`.

**Forgot password** — Body: `{ "email": "..." }`. Always returns 200 with a generic message; does not reveal if the email exists.

**Reset password** — Body: `{ "recoverString": "...", "newPassword": "..." }`. New password min length 8. Returns 400 with `INVALID_RECOVERY` if token is missing or expired.

**Auth response** (register/login/refresh):

```json
{
  "accessToken": "...",
  "refreshToken": "...",
  "expiresAtUtc": "2025-02-25T12:00:00Z",
  "tokenType": "Bearer",
  "displayName": "John Doe"
}
```

### Users endpoints (authenticated)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/users/{id}` | Bearer (self or Admin) | Get one user (safe fields only) |
| `GET` | `/api/users` | Bearer, Admin | Paginated list; query: `pageNumber`, `pageSize` |
| `PUT` | `/api/users/{id}` | Bearer (self or Admin) | Update profile: users own email and/or displayName; Admin can set Email, DisplayName, Role, IsActive |
| `PATCH` | `/api/users/{id}/lock` | Bearer, Admin | Lock user (login returns 403 until lockout ends). Optional body: `{ "lockoutEnd": "UTC datetime" }`; if omitted, lock 15 min. Cannot lock yourself. Returns 204. |
| `PATCH` | `/api/users/{id}/unlock` | Bearer, Admin | Clear user lockout (failed attempts and lockout end). Returns 204. |
| `DELETE` | `/api/users/{id}` | Bearer, Admin | Soft-delete user (sets IsDeleted, DeletedAt). Returns 204. Cannot delete yourself. |
| `PATCH` | `/api/users/{id}/restore` | Bearer, Admin | Restore soft-deleted user. Returns 204. |
| `DELETE` | `/api/users/{id}/hard` | Bearer, Admin | **Dangerous:** Permanently delete user and refresh tokens. Requires `?confirm=true`. Cannot delete yourself. Returns 204. |

**Get user** — Returns `UserResponse`: `id`, `email`, `displayName`, `role`, `isActive`, `createdAtUtc`, `updatedAtUtc`. No password/recovery data.

**Get users** — Query: `?pageNumber=1&pageSize=10`. Response: `items`, `totalCount`, `pageNumber`, `pageSize`.

**Update user** — Body: `{ "email": "...", "displayName": "...", "role": "...", "isActive": true }`. Omit fields to leave unchanged. Users can update own email and/or displayName; Admin only for `role` and `isActive`.

**Delete user** — No body. Admin only. Soft-delete (IsDeleted = 1, DeletedAt = now). Returns 204; 404 if not found; 400 if already deleted or self-delete.

**Restore user** — No body. Admin only. Sets IsDeleted = 0, DeletedAt = NULL. Returns 204; 404 if not found; 400 if not deleted. All GET queries exclude soft-deleted users.

**Lock user** — Optional body: `{ "lockoutEnd": "2025-02-26T15:30:00Z" }`. Admin only. Sets lockout so login returns 403 until the given UTC time. If `lockoutEnd` is omitted, lockout ends in 15 minutes. `lockoutEnd` must be in the future (400 `INVALID_LOCKOUT_END` otherwise). Cannot lock yourself (400 `CANNOT_LOCK_SELF`). Returns 204; 404 if not found.

**Unlock user** — No body. Admin only. Clears FailedLoginAttempts and LockoutEnd. Returns 204; 404 if not found.

**Hard delete user** — No body. Admin only. Query: `?confirm=true` (required). Permanently deletes the user and their refresh tokens. Returns 204; 400 if `confirm` is not true or self-delete; 404 if not found. Soft delete remains the default; use this only when permanent removal is required.

---

## Configuration

Secrets and environment-specific settings are **not** committed. Use the example file and local config:

1. Copy `api/Bangkok.Api/appsettings.Example.json` to `api/Bangkok.Api/appsettings.json`.
2. Edit `appsettings.json` (or use User Secrets / environment variables) with:
   - **ConnectionStrings:DefaultConnection** — SQL Server connection string.
   - **Jwt:SigningKey** — Symmetric key for JWT (e.g. 32+ characters).
   - **Jwt:Issuer**, **Jwt:Audience** — Token issuer and audience.
   - **Jwt:AccessTokenExpirationMinutes**, **Jwt:RefreshTokenExpirationDays** — Token lifetimes.
   - **Cors:AllowedOrigins** — Allowed origins (e.g. `["http://localhost:3000"]`); **Cors:AllowedHeaders**, **Cors:AllowedMethods** optional.
   - **Serilog** — Minimum level, overrides, sinks (e.g. file path `Logs/log-.txt`).

Example structure (values are placeholders in the repo):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;User Id=...;Password=...;Database=BangkokDb;TrustServerCertificate=True;Encrypt=False"
  },
  "Jwt": {
    "Issuer": "Bangkok.Api",
    "Audience": "Bangkok.Client",
    "SigningKey": "YOUR_SECURE_KEY_AT_LEAST_32_CHARACTERS",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "Serilog": { ... },
  "Logging": { ... }
}
```

---

## Getting started

**Prerequisites:** .NET 10 SDK, SQL Server (or compatible instance).

1. Clone the repository.
2. Copy `api/Bangkok.Api/appsettings.Example.json` to `api/Bangkok.Api/appsettings.json` and set connection string and JWT signing key.
3. Apply database schema: run `sql/001_initial.sql` (and any `sql/alters/*.sql`) against your database.
4. From the `api` folder:
   ```bash
   dotnet run --project Bangkok.Api
   ```
   Or open `Bangkok.sln` in Visual Studio / Rider and run **Bangkok.Api**.

5. In Development, Swagger UI: **https://localhost:&lt;port&gt;/swagger** (port from `launchSettings.json`).

Sample request bodies for auth are in the `json/` folder (e.g. `login.json`, `register.json`, `refresh.json`, `revoke.json`).

---

## Docker / Linux deployment

The solution includes a production-ready multi-stage **Dockerfile** (in `api/`) and **docker-compose.yml** (in the repo root) for Linux servers, local development, and VPS deployment.

**Prerequisites:** Docker and Docker Compose on the host (e.g. Ubuntu/Debian).

### Build and run

1. **Configure environment**

   Copy the example env file and set secrets (do not commit `.env`):

   ```bash
   cp .env.example .env
   ```

   Edit `.env`: set `MSSQL_SA_PASSWORD` and use the same value in `ConnectionStrings__DefaultConnection` (password in the connection string). Set `Jwt__SigningKey` to a secure key (at least 32 characters). The API connects to SQL Server using the hostname **`sqlserver`** (the compose service name).

2. **Build the image**

   ```bash
   docker compose build
   ```

3. **Start services**

   ```bash
   docker compose up -d
   ```

   The API listens on **port 8080**; SQL Server on **1433**. The API container waits for the SQL Server container to start (`depends_on`). SQL Server may take 20–30 seconds to accept connections; the app will connect when the database is ready.

4. **Stop services**

   ```bash
   docker compose down
   ```

   Add `-v` to remove named volumes (e.g. database data): `docker compose down -v`.

5. **View logs**

   ```bash
   docker compose logs -f
   ```

   API only: `docker compose logs -f bangkok-api`.

### Compose services

| Service       | Image / build        | Port  | Description                          |
|---------------|----------------------|------|--------------------------------------|
| `bangkok-api` | Build from `api/`    | 8080 | ASP.NET Core API (non-root, Release) |
| `sqlserver`   | mcr.microsoft.com/mssql/server:2022-latest | 1433 | SQL Server (Linux), data in volume   |

### Environment variables (API)

| Variable                             | Purpose                    |
|--------------------------------------|----------------------------|
| `ASPNETCORE_ENVIRONMENT`             | e.g. Production            |
| `ConnectionStrings__DefaultConnection` | SQL connection string (use `Server=sqlserver`) |
| `Jwt__SigningKey`                    | JWT signing key (32+ chars)|
| `Jwt__Issuer`                        | Token issuer               |
| `Jwt__Audience`                      | Token audience             |

### Logs and volumes

- **API logs:** Serilog writes to **`/app/Logs`** inside the container. The compose file mounts a named volume `bangkok-logs` at `/app/Logs` so logs persist and can be inspected (e.g. `docker compose exec bangkok-api cat /app/Logs/log-*.txt` or bind-mount a host path if preferred).
- **SQL data:** Stored in the named volume `sqlserver-data` so the database persists across restarts.

### Notes

- **No Kubernetes, Redis, or reverse proxy** — single compose stack, suitable for startup-level production.
- **Swagger** is disabled when `ASPNETCORE_ENVIRONMENT=Production`; set to `Development` in `.env` only if you need Swagger in the container (not recommended on a public host).

---

## Health endpoints

| Endpoint | Purpose | Checks |
|----------|---------|--------|
| `GET /health` | Full health | All (e.g. SQL) |
| `GET /health/ready` | Readiness (e.g. load balancer / K8s) | Tagged "ready" (SQL) |
| `GET /health/live` | Liveness (e.g. K8s) | None |

Response format (JSON):

```json
{
  "status": "Healthy",
  "totalDuration": 12.5,
  "entries": [
    {
      "name": "sql",
      "status": "Healthy",
      "description": null,
      "duration": 8.2
    }
  ]
}
```

---

## Development

- **Swagger** is enabled only in the Development environment.
- **Middleware order:** Correlation ID → Request ID enricher → Exception handling → Rate limiter → Authentication → Authorization.
- **Brute force:** In-memory only (no DB/Redis). Tracks IP, email, and IP+email; thresholds 10/5/5 in 5 min. IP escalation: 30min, 2h, 24h when re-triggered within 24h. Singleton `IIpBlockService` in Api; 429 responses include Retry-After.
- **Logs:** Serilog writes to console and to `Logs/log-YYYYMMDD.txt` (path configurable in `appsettings.json`). Correlation ID and request ID are attached for tracing.
- **API response model:** Use `ApiResponse<T>.Ok(data, correlationId)` and `ApiResponse<T>.Fail(error, correlationId)` so all endpoints return a consistent shape.
