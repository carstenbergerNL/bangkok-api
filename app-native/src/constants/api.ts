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
  BILLING: {
    USAGE: '/api/Billing/usage',
    PLANS: '/api/Billing/plans',
    CREATE_CHECKOUT_SESSION: '/api/Billing/create-checkout-session',
  },
  TENANT_MODULES: {
    BASE: '/api/tenant/modules',
    MANAGEMENT: '/api/tenant/modules/management',
    SET_ACTIVE: (moduleKey: string) => `/api/tenant/modules/${moduleKey}/active`,
    MODULE_USERS: (moduleKey: string) => `/api/tenant/modules/${moduleKey}/users`,
    MODULE_USER_REVOKE: (moduleKey: string, userId: string) => `/api/tenant/modules/${moduleKey}/users/${userId}`,
  },
  TASKS_MODULE: {
    BASE: '/api/tasks-module',
    MY: '/api/tasks-module/my',
    BY_ID: (id: string) => `/api/tasks-module/${id}`,
    STATUS: (id: string) => `/api/tasks-module/${id}/status`,
  },
  PLATFORM_ADMIN: {
    DASHBOARD_STATS: '/api/PlatformAdmin/dashboard/stats',
    TENANTS: '/api/PlatformAdmin/tenants',
    TENANT_USAGE: (id: string) => `/api/PlatformAdmin/tenants/${id}/usage`,
    TENANT_SUSPEND: (id: string) => `/api/PlatformAdmin/tenants/${id}/suspend`,
    TENANT_STATUS: (id: string) => `/api/PlatformAdmin/tenants/${id}/status`,
    TENANT_UPGRADE: (id: string) => `/api/PlatformAdmin/tenants/${id}/upgrade`,
  },
} as const;
