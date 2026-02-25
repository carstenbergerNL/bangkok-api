# Bangkok API

.NET Web API with JWT authentication, Dapper, and SQL Server.

## Setup

1. Copy `api/Bangkok.Api/appsettings.Example.json` to `api/Bangkok.Api/appsettings.json`.
2. Edit `appsettings.json` with your connection string and JWT signing key.
3. Run the API from `api` folder: `dotnet run --project Bangkok.Api`.

## Health

- `GET /health` — all checks (includes DB)
- `GET /health/ready` — readiness (DB)
- `GET /health/live` — liveness
