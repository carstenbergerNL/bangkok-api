/**
 * API path constants. Must stay in sync with backend controllers and app (web).
 */
export const API_PATHS = {
  AUTH: {
    LOGIN: '/api/Auth/login',
    REGISTER: '/api/Auth/register',
    REFRESH: '/api/Auth/refresh',
    REVOKE: '/api/Auth/revoke',
    FORGOT_PASSWORD: '/api/Auth/forgot-password',
    RESET_PASSWORD: '/api/Auth/reset-password',
    CHANGE_PASSWORD: '/api/Auth/change-password',
  },
  USERS: {
    BASE: '/api/Users',
    BY_ID: (id: string) => `/api/Users/${id}`,
    RESTORE: (id: string) => `/api/Users/${id}/restore`,
    LOCK: (id: string) => `/api/Users/${id}/lock`,
    UNLOCK: (id: string) => `/api/Users/${id}/unlock`,
    HARD_DELETE: (id: string) => `/api/Users/${id}/hard`,
  },
  PROFILE: {
    BASE: '/api/Profile',
    BY_USER_ID: (userId: string) => `/api/Profile/${userId}`,
  },
  ROLES: {
    BASE: '/api/Roles',
    BY_ID: (id: string) => `/api/Roles/${id}`,
    ASSIGN_TO_USER: (userId: string, roleId: string) => `/api/Users/${userId}/roles/${roleId}`,
  },
} as const;
