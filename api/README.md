# Bangkok API

ASP.NET Core Web API for the Bangkok product. Single backend for the web and native clients; all endpoints use the same request/response contracts.

## Tech stack

- **.NET 10** – ASP.NET Core Web API
- **SQL Server** – Persistence (see `../sql/` for schema and migrations)
- **JWT** – Bearer authentication; refresh tokens
- **Dapper** – Data access (repositories)
- **Serilog** – Structured logging
- **Swagger / OpenAPI** – API docs at `/swagger` when running

## Solution structure

```
api/
├── Bangkok.Api/           # Web API project (controllers, middleware, Program.cs)
├── Bangkok.Application/   # DTOs, interfaces (services, repositories)
├── Bangkok.Domain/        # Entities (User, Project, Task, etc.)
├── Bangkok.Infrastructure/# Implementations (repositories, services, DI)
└── README.md
```

## Prerequisites

- **.NET 10 SDK**
- **SQL Server** (LocalDB, Express, or full) with a database created for Bangkok

## Configuration

1. Copy `Bangkok.Api/appsettings.Example.json` to `Bangkok.Api/appsettings.json` (or use environment-specific files).
2. Set **ConnectionStrings:DefaultConnection** to your SQL Server connection string.
3. Configure **Jwt**, **Cors** (e.g. allowed origins for the web app), and other sections as in the example.

`appsettings.json` is gitignored; do not commit secrets.

## Run

From the `api/` directory:

```bash
dotnet run --project Bangkok.Api/Bangkok.Api.csproj
```

Or open the solution in Visual Studio and run **Bangkok.Api**.

- **HTTPS:** https://localhost:56028  
- **HTTP:** http://localhost:56029  
- **Swagger UI:** https://localhost:56028/swagger  

Ensure the database exists and migrations (in `../sql/`) have been applied.

## Main controllers

| Controller        | Description |
|-------------------|-------------|
| **Auth**          | Login, register, refresh, revoke; forgot/reset password. |
| **Users**         | CRUD, soft-delete, restore, lock/unlock, hard delete; mention search. |
| **Profile**       | Get, create, update user profile. |
| **Roles**         | CRUD; assign permissions to roles. |
| **Permissions**   | CRUD. |
| **Projects**      | CRUD; members, labels, custom fields, dashboard, **export CSV**, **automation rules**. |
| **Tasks**         | CRUD; comments, activities, time logs, attachments. |
| **Comments**      | Update, delete (task comments). |
| **Timelogs**      | Delete time log. |
| **Notifications** | List, unread count, mark read. |
| **Attachments**   | Download task attachment. |
| **ProjectTemplates** | CRUD project templates. |

All authenticated endpoints require `Authorization: Bearer <token>`. Permission checks (e.g. Project.View, Task.Edit) are enforced in services.

## Automation rules

Project-level rules run inside **TaskService** when tasks are created or updated:

- **Triggers:** TaskCompleted, TaskOverdue, TaskAssigned  
- **Actions:** NotifyUser (target user), ChangeStatus (target value), AddLabel (label id)  

Configured via **GET/POST/DELETE** `api/Projects/{projectId}/automation-rules`. See `../sql/README.md` for the `ProjectAutomationRule` table.

## Database

Schema and migration scripts live in **`../sql/`**. Use `001_initial.sql` for a new database; apply scripts in `alters/` in order for existing databases.
