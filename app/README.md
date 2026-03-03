# Bangkok Admin App

React frontend for the Bangkok API. Provides login, dashboard, **project and task management** (Kanban, labels, members, time tracking), settings, **roles and permissions**, and **admin-only** user management (CRUD, restore deleted users, lock/unlock).

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
│   ├── api/              # API client (base URL, 401 handling)
│   ├── assets/
│   ├── components/       # AdminRoute, Modal, PrivateRoute, PermissionRoute, Toast, etc.
│   ├── constants/        # api.ts, validation, permissions
│   ├── context/          # AuthContext (user, token, login, logout)
│   ├── hooks/            # useDarkMode, usePermissions
│   ├── layouts/          # MainLayout, Sidebar, Topbar
│   ├── models/           # ApiResponse, User, LoginRequest, Profile, Role, Permission, etc.
│   ├── modules/
│   │   └── projects/     # projectService, taskService, labelService, memberService,
│   │                     # commentService, activityService, dashboardService;
│   │                     # ProjectListPage, ProjectDetailsPage, KanbanBoard, TaskDrawer,
│   │                     # TaskFormModal, TaskList, ProjectFormModal, ProjectMembersTab,
│   │                     # ProjectDashboardTab, ProjectLabelsSettings, types
│   ├── pages/            # Login, Dashboard, Profile, Roles, AdminSettings (Users, Roles, Permissions)
│   ├── services/         # authService, userService, roleService, permissionService,
│   │                     # profileService, notificationService
│   ├── utils/            # toast
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

| Command         | Description                      |
|-----------------|----------------------------------|
| `npm run dev`   | Start dev server (Vite, HMR)     |
| `npm run build` | TypeScript check + production build |
| `npm run preview` | Serve production build locally  |
| `npm run lint`  | Run ESLint                       |

**Dev server:** by default Vite runs at `http://localhost:5173`. In development, the app uses a proxy to `VITE_API_BASE_URL` (see `vite.config.ts`) so the browser talks to the same origin and avoids CORS during local dev.

## Features

- **Authentication**
  - Login with email and password (JWT).
  - Token and user (including roles/permissions) stored in `localStorage`; 401 from API triggers logout and redirect to login.

- **Layout**
  - **Topbar:** app title, hamburger (toggle sidebar), dark/light toggle, notifications, user dropdown (logout).
  - **Sidebar:** Dashboard, Projects, Profile, Settings; **Roles** (permission-based); **Admin Settings** for users with Admin role.

- **Projects**
  - **Project list** – List projects; create, edit, delete. Permission-based: Project.View, Project.Create, Project.Edit, Project.Delete.
  - **Project details** – Tabs: **Board** (Kanban), **Tasks**, **Dashboard**, **Members**, **Labels**, **Settings**. Settings sub-tabs: **Badges** (labels), **Custom Fields**, **Automation** (lightweight rules: when task completed/overdue/assigned → notify user, change status, or add label). Dashboard shows total/completed/overdue tasks, total estimated hours, total logged hours, over-budget indicator. **Export** button in the header downloads project tasks as CSV (title, status, priority, assignee, due date, labels, logged hours); shows loading state and triggers file download.

- **Tasks**
  - **Kanban** – Columns by status (ToDo, In Progress, Done); drag-and-drop; create task, open task drawer.
  - **Task drawer** – Details tab: title, description, status, priority, assignee, due date, **estimated hours**, labels; **Time tracking**: log hours + description, list of time logs, total logged, over-budget indicator; Comments tab; Activity tab. Edit/delete when permitted (Task.Edit, Task.Delete).
  - **Task list** – Table view with filters (status, priority, assignee, label, due date, search).
  - Create/update task: title, description, status, priority, assignee, due date, **estimated hours**, labels.

- **Time tracking**
  - Per task: log hours with optional description; view all time logs (user, hours, date); delete own/logs when Task.Edit. Total logged vs estimated hours; over-budget warning when logged > estimated.

- **Roles and permissions**
  - **Roles** – List roles; create, edit, delete; assign permissions to roles. Permission-based access.
  - **Permissions** – List permissions; assign to roles.

- **Admin Settings** (admin only)
  - **Users:** list (email, display name, role, active), **Add user** (register), **Edit** (email, display name, role, active; cannot change own “Active”), **Delete** (soft-delete; cannot delete yourself), **Lock** / **Unlock**, assign roles.
  - **Deleted users:** list of soft-deleted users with **Restore** and “Deleted at” time.
  - **Hard delete** (optional): permanently remove a user with confirm.

- **Guards**
  - **PrivateRoute** – Redirects unauthenticated users to `/login`.
  - **AdminRoute** – Redirects non-admin users; used for `/admin-settings`.
  - **PermissionRoute** – Checks specific permissions (e.g. Project.View, Task.Create).

## API usage

- All authenticated requests send `Authorization: Bearer <token>`.
- **Auth:** Login `POST /api/Auth/login`; Register `POST /api/Auth/register`; Refresh `POST /api/Auth/refresh`; Revoke `POST /api/Auth/revoke`; Forgot password, Reset password, Change password.
- **Users:** List `GET /api/Users`; Get `GET /api/Users/{id}`; Update `PUT /api/Users/{id}`; Soft-delete `DELETE /api/Users/{id}`; Restore `PATCH /api/Users/{id}/restore`; Lock/Unlock; Hard delete `DELETE /api/Users/{id}/hard?confirm=true`; Mention search `GET /api/Users/mention-search`.
- **Profile:** Get `GET /api/Profile/{userId}`; Create/Update `POST /api/Profile`, `PUT /api/Profile/{userId}`.
- **Roles:** List `GET /api/Roles`; CRUD; role permissions GET/POST/DELETE.
- **Permissions:** List `GET /api/Permissions`; CRUD.
- **Projects:** List `GET /api/Projects`; Get `GET /api/Projects/{id}`; Create `POST /api/Projects`; Update `PUT /api/Projects/{id}`; Delete `DELETE /api/Projects/{id}`; Dashboard `GET /api/Projects/{id}/dashboard`; **Export** `GET /api/Projects/{id}/export` (returns CSV); **Automation rules** `GET /api/Projects/{id}/automation-rules`, `POST /api/Projects/{id}/automation-rules`, `DELETE /api/Projects/{id}/automation-rules/{ruleId}`; Members GET/POST/PUT/DELETE; Labels GET/POST/DELETE; Custom fields GET/POST/PUT/DELETE.
- **Tasks:** List by project `GET /api/Tasks?projectId=...`; Get `GET /api/Tasks/{id}`; Create `POST /api/Tasks`; Update `PUT /api/Tasks/{id}`; Delete `DELETE /api/Tasks/{id}`; Comments `GET/POST /api/Tasks/{taskId}/comments`; Activities `GET /api/Tasks/{taskId}/activities`; **Time logs** `GET /api/Tasks/{taskId}/timelogs`, `POST /api/Tasks/{taskId}/timelogs`; Delete time log `DELETE /api/Timelogs/{id}`.
- **Comments:** Update `PUT /api/Comments/{id}`; Delete `DELETE /api/Comments/{id}` (task comments by comment id).
- **Notifications:** List `GET /api/Notifications`; Unread count `GET /api/Notifications/unread-count`; Mark read `PUT /api/Notifications/{id}/read`; Mark all read `PUT /api/Notifications/read-all`.

## Build for production

```bash
npm run build
```

Output is in `dist/`. Serve with any static host; ensure the host is allowed in the API’s CORS and that `VITE_API_BASE_URL` points to the real API URL used in production.
