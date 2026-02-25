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
- [Health endpoints](#health-endpoints)
- [Development](#development)

---

## Features

- **JWT authentication** — Access tokens (short-lived) and refresh tokens (long-lived), with refresh tokens stored in SQL Server
- **User registration and login** — Email/password with hashed passwords and salt
- **Token refresh and revoke** — Exchange refresh token for new tokens; revoke refresh tokens (authenticated)
- **Password recovery** — Forgot-password (generic success, no email enumeration); reset-password with secure recovery string and expiry
- **User profile and list** — Get/update user (safe fields only); users can update own email; Admin can list all (paginated), update Role and IsActive
- **Health checks** — Liveness, readiness, and full health with SQL Server check
- **Structured logging** — Serilog with console and file sinks, correlation ID and request ID enrichers
- **Global error handling** — Unhandled exceptions return a consistent `ApiResponse` with correlation ID
- **Swagger/OpenAPI** — In Development, with Bearer JWT support
- **Options pattern** — Strongly typed configuration (JWT, connection strings, Serilog)

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
│   ├── Bangkok.Api/           # Web API project
│   │   ├── Controllers/       # AuthController, UsersController
│   │   ├── Middleware/        # CorrelationId, RequestIdEnricher, ExceptionHandling
│   │   ├── Program.cs
│   │   ├── appsettings.Example.json
│   │   └── Properties/launchSettings.json
│   ├── Bangkok.Application/   # Interfaces, DTOs, options, response models
│   ├── Bangkok.Domain/        # User, RefreshToken entities
│   └── Bangkok.Infrastructure/# Repositories, AuthService, JwtService, PasswordHasher, DI
├── sql/
│   ├── 001_initial.sql        # User + RefreshToken tables
│   └── alters/               # Schema change scripts (002–004: display name, password recovery, is_active)
├── json/                      # Example request bodies (login, register, refresh, revoke, forgot_password, reset_password, update_user, get_users)
├── .cursor/rules/             # Architecture rules
├── .gitignore
└── README.md
```

---

## Database

- **Server:** SQL Server
- **Conventions:** `UNIQUEIDENTIFIER` for primary keys, `DATETIME2(7)` for dates, `NVARCHAR` for strings.

**Tables:**

- **User** — `Id`, `Email` (unique), `DisplayName`, `PasswordHash`, `PasswordSalt`, `Role`, `IsActive`, `CreatedAtUtc`, `UpdatedAtUtc`, `RecoverString`, `RecoverStringExpiry` (DisplayName and recovery columns via alters on existing DBs).
- **RefreshToken** — `Id`, `UserId` (FK), `Token`, `ExpiresAtUtc`, `CreatedAtUtc`, `RevokedReason`, `RevokedAtUtc`.

Apply schema:

1. Create a database (e.g. `BangkokDb`).
2. Run `sql/001_initial.sql`.
3. Run any scripts in `sql/alters/` in order (e.g. `002_add_user_display_name.sql`, `003_add_password_recovery.sql`, `004_add_user_is_active.sql`).

---

## API overview

Base path: **`/api`**. All success/error responses use the wrapper:

```json
{
  "success": true,
  "data": { ... },
  "error": null,
  "correlationId": "..."
}
```

Errors:

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "INVALID_CREDENTIALS",
    "message": "Invalid email or password.",
    "details": null
  },
  "correlationId": "..."
}
```

### Auth endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/auth/register` | No | Register user; returns access + refresh tokens |
| `POST` | `/api/auth/login` | No | Login; returns access + refresh tokens |
| `POST` | `/api/auth/refresh` | No | Exchange refresh token for new access + refresh tokens |
| `POST` | `/api/auth/revoke` | Bearer | Revoke a refresh token |
| `POST` | `/api/auth/forgot-password` | No | Request password recovery; always returns generic success (no email enumeration) |
| `POST` | `/api/auth/reset-password` | No | Reset password with recovery string from forgot-password |

**Register** — Body: `{ "email": "...", "password": "...", "displayName": "...", "role": "User" }`. Password min length 8. `displayName` optional.

**Login** — Body: `{ "email": "...", "password": "..." }`.

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

**Get user** — Returns `UserResponse`: `id`, `email`, `displayName`, `role`, `isActive`, `createdAtUtc`, `updatedAtUtc`. No password/recovery data.

**Get users** — Query: `?pageNumber=1&pageSize=10`. Response: `items`, `totalCount`, `pageNumber`, `pageSize`.

**Update user** — Body: `{ "email": "...", "displayName": "...", "role": "...", "isActive": true }`. Omit fields to leave unchanged. Users can update own email and/or displayName; Admin only for `role` and `isActive`.

---

## Configuration

Secrets and environment-specific settings are **not** committed. Use the example file and local config:

1. Copy `api/Bangkok.Api/appsettings.Example.json` to `api/Bangkok.Api/appsettings.json`.
2. Edit `appsettings.json` (or use User Secrets / environment variables) with:
   - **ConnectionStrings:DefaultConnection** — SQL Server connection string.
   - **Jwt:SigningKey** — Symmetric key for JWT (e.g. 32+ characters).
   - **Jwt:Issuer**, **Jwt:Audience** — Token issuer and audience.
   - **Jwt:AccessTokenExpirationMinutes**, **Jwt:RefreshTokenExpirationDays** — Token lifetimes.
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
- **Middleware order:** Correlation ID → Request ID enricher → Exception handling → Authentication → Authorization.
- **Logs:** Serilog writes to console and to `Logs/log-YYYYMMDD.txt` (path configurable in `appsettings.json`). Correlation ID and request ID are attached for tracing.
- **API response model:** Use `ApiResponse<T>.Ok(data, correlationId)` and `ApiResponse<T>.Fail(error, correlationId)` so all endpoints return a consistent shape.
