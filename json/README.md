# JSON request examples

Sample request bodies for **Bangkok API** endpoints. Use them for manual testing (e.g. Postman, curl), documentation, or as a reference for the frontend.

## Files and endpoints

| File | Method + endpoint | Description |
|------|-------------------|-------------|
| **login.json** | `POST /api/Auth/login` | Email and password. After 5 failed attempts the account is locked (403). |
| **register.json** | `POST /api/Auth/register` | Email, password, optional displayName, role (default User). |
| **refresh.json** | `POST /api/Auth/refresh` | Refresh token in body. |
| **revoke.json** | `POST /api/Auth/revoke` | Refresh token to revoke. |
| **forgot_password.json** | `POST /api/Auth/forgot-password` | Email for password recovery. |
| **reset_password.json** | `POST /api/Auth/reset-password` | Token and new password. |
| **get_users.json** | `GET /api/Users` | Query params example (pageNumber, pageSize); GET has no body. |
| **update_user.json** | `PUT /api/Users/{id}` | Optional: email, displayName, role, isActive. Admin can set all; users can update own email/displayName only. |
| **delete_user.json** | `DELETE /api/Users/{id}` | No body. Admin only. Soft-deletes the user. |
| **restore_user.json** | `PATCH /api/Users/{id}/restore` | No body. Admin only. Restores a soft-deleted user. |
| **hard_delete_user.json** | `DELETE /api/Users/{id}/hard?confirm=true` | No body (or optional). Admin only. Requires `confirm=true` query. |

## Usage

- **Auth endpoints** – No `Authorization` header for login/register; use Bearer token for refresh, revoke, and all `/api/Users` calls.
- Some files include a `_comment` field for documentation; strip it before sending if your client doesn’t allow unknown properties.
- Replace placeholders (email, password, ids) with real values when testing.
