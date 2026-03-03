# JSON request examples

Sample request bodies for **Bangkok API** endpoints. Use them for manual testing (e.g. Postman, curl), documentation, or as a reference for the frontend. All authenticated endpoints require `Authorization: Bearer <token>`.

## Files and endpoints

### Auth (`/api/Auth`)

| File | Method + endpoint | Description |
|------|-------------------|-------------|
| **login.json** | `POST /api/Auth/login` | Email and password. After 5 failed attempts the account is locked (403). Rate limited. |
| **register.json** | `POST /api/Auth/register` | Email, password, optional displayName, role (default User). |
| **refresh.json** | `POST /api/Auth/refresh` | Refresh token in body. |
| **revoke.json** | `POST /api/Auth/revoke` | Refresh token to revoke. |
| **forgot_password.json** | `POST /api/Auth/forgot-password` | Email for password recovery. |
| **reset_password.json** | `POST /api/Auth/reset-password` | Token and new password. |

### Users (`/api/Users`)

| File | Method + endpoint | Description |
|------|-------------------|-------------|
| **get_users.json** | `GET /api/Users` | Query params example (pageNumber, pageSize, includeDeleted); GET has no body. |
| **update_user.json** | `PUT /api/Users/{id}` | Optional: email, displayName, role, isActive. Admin can set all; users can update own email/displayName only. |
| **delete_user.json** | `DELETE /api/Users/{id}` | No body. Admin only. Soft-deletes the user. |
| **restore_user.json** | `PATCH /api/Users/{id}/restore` | No body. Admin only. Restores a soft-deleted user. |
| **hard_delete_user.json** | `DELETE /api/Users/{id}/hard?confirm=true` | No body (or optional). Admin only. Requires `confirm=true` query. |

### Profile (`/api/Profile`)

| Method + endpoint | Description |
|-------------------|-------------|
| `GET /api/Profile/{userId}` | Get profile; no body. |
| `POST /api/Profile` | Create profile (userId, firstName, lastName, etc.). |
| `PUT /api/Profile/{userId}` | Update profile. |
| `DELETE /api/Profile/{userId}` | Delete profile; no body. |

### Roles (`/api/Roles`)

| Method + endpoint | Description |
|-------------------|-------------|
| `GET /api/Roles` | List roles; no body. |
| `GET /api/Roles/{id}` | Get role; no body. |
| `POST /api/Roles` | Create role (name, description). |
| `PUT /api/Roles/{id}` | Update role. |
| `DELETE /api/Roles/{id}` | Delete role; no body. |
| `GET /api/Roles/{id}/permissions` | List permissions for role. |
| `POST /api/Roles/{id}/permissions/{permissionId}` | Assign permission to role. |
| `DELETE /api/Roles/{id}/permissions/{permissionId}` | Remove permission from role. |

### Permissions (`/api/Permissions`)

| Method + endpoint | Description |
|-------------------|-------------|
| `GET /api/Permissions` | List permissions; no body. |
| `GET /api/Permissions/{id}` | Get permission; no body. |
| `POST /api/Permissions` | Create permission (name, description). |
| `PUT /api/Permissions/{id}` | Update permission. |
| `DELETE /api/Permissions/{id}` | Delete permission; no body. |

### Projects (`/api/Projects`)

| File | Method + endpoint | Description |
|------|-------------------|-------------|
| **create_project.json** | `POST /api/Projects` | Name, description, status. Requires Project.Create. |
| **update_project.json** | `PUT /api/Projects/{id}` | Optional: name, description, status. Requires Project.Edit. |
| `GET /api/Projects` | List projects (optional query: status); no body. |
| `GET /api/Projects/{id}` | Get project; no body. |
| `DELETE /api/Projects/{id}` | Delete project; no body. Fails if project has tasks. |
| `GET /api/Projects/{id}/dashboard` | Dashboard stats (tasks, total estimated hours, total logged hours, over-budget count); no body. |
| `GET /api/Projects/{id}/members/me` | Current user's role; no body. |
| `GET /api/Projects/{id}/members` | List members; no body. |
| `POST /api/Projects/{id}/members` | Add member (userId, role). |
| `PUT /api/Projects/{id}/members/{memberId}` | Update member role. |
| `DELETE /api/Projects/{id}/members/{memberId}` | Remove member; no body. |
| `GET /api/Projects/{id}/labels` | List labels; no body. |
| `POST /api/Projects/{id}/labels` | Create label (name, color). |
| `DELETE /api/Projects/{id}/labels/{labelId}` | Delete label; no body. |

### Tasks (`/api/Tasks`)

| File | Method + endpoint | Description |
|------|-------------------|-------------|
| **create_task.json** | `POST /api/Tasks` | projectId, title, description, status, priority, assignedToUserId, dueDate, estimatedHours, labelIds. Requires Task.Create. |
| **update_task.json** | `PUT /api/Tasks/{id}` | Optional: title, description, status, priority, assignedToUserId, dueDate, estimatedHours, labelIds. Requires Task.Edit. |
| `GET /api/Tasks?projectId=...` | List tasks (required projectId; optional status, priority, assignedToUserId, labelId, dueBefore, dueAfter, search); no body. |
| `GET /api/Tasks/{id}` | Get task; no body. |
| `DELETE /api/Tasks/{id}` | Delete task; no body. Requires Task.Delete. |
| `GET /api/Tasks/{taskId}/comments` | List task comments; no body. |
| `POST /api/Tasks/{taskId}/comments` | Add comment (content). Requires Task.Comment. |
| `PUT /api/Comments/{id}` | Update comment (content). Owner only. |
| `DELETE /api/Comments/{id}` | Delete comment; no body. Owner or admin. |
| `GET /api/Tasks/{taskId}/activities` | List task activities; no body. Requires Task.ViewActivity. |
| `GET /api/Tasks/{taskId}/timelogs` | List time logs for task; no body. Requires Task.View. |
| **create_timelog.json** | `POST /api/Tasks/{taskId}/timelogs` | hours (decimal), optional description. Requires Task.Edit. |

### Timelogs (`/api/Timelogs`)

| Method + endpoint | Description |
|-------------------|-------------|
| `DELETE /api/Timelogs/{id}` | Delete a time log entry. No body. Requires Task.Edit and project membership. |

### Notifications (`/api/Notifications`)

| Method + endpoint | Description |
|-------------------|-------------|
| `GET /api/Notifications` | List user's notifications; no body. |
| `GET /api/Notifications/unread-count` | Unread count; no body. |
| `PUT /api/Notifications/{id}/read` | Mark one as read; no body. |
| `PUT /api/Notifications/read-all` | Mark all as read; no body. |

## Usage

- **Auth endpoints** – No `Authorization` header for login/register; use Bearer token for refresh, revoke, and all other API calls.
- Some files include a `_comment` field for documentation; strip it before sending if your client doesn’t allow unknown properties.
- Replace placeholders (email, password, ids) with real values when testing.
