/**
 * API path constants. Must stay in sync with backend controllers and app-native.
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
    REMOVE_FROM_USER: (userId: string, roleId: string) => `/api/Users/${userId}/roles/${roleId}`,
    PERMISSIONS: (roleId: string) => `/api/Roles/${roleId}/permissions`,
    ASSIGN_PERMISSION: (roleId: string, permissionId: string) => `/api/Roles/${roleId}/permissions/${permissionId}`,
  },
  PERMISSIONS: {
    BASE: '/api/Permissions',
    BY_ID: (id: string) => `/api/Permissions/${id}`,
  },
  PROJECTS: {
    BASE: '/api/Projects',
    BY_ID: (id: string) => `/api/Projects/${id}`,
    FROM_TEMPLATE: (templateId: string) => `/api/Projects/from-template/${templateId}`,
    MEMBERS: (projectId: string) => `/api/Projects/${projectId}/members`,
    MEMBERS_ME: (projectId: string) => `/api/Projects/${projectId}/members/me`,
    MEMBER_BY_ID: (projectId: string, memberId: string) => `/api/Projects/${projectId}/members/${memberId}`,
    LABELS: (projectId: string) => `/api/Projects/${projectId}/labels`,
    LABEL_BY_ID: (projectId: string, labelId: string) => `/api/Projects/${projectId}/labels/${labelId}`,
    CUSTOM_FIELDS: (projectId: string) => `/api/Projects/${projectId}/custom-fields`,
    CUSTOM_FIELD_BY_ID: (projectId: string, fieldId: string) => `/api/Projects/${projectId}/custom-fields/${fieldId}`,
    DASHBOARD: (projectId: string) => `/api/Projects/${projectId}/dashboard`,
    EXPORT: (projectId: string) => `/api/Projects/${projectId}/export`,
  },
  TASKS: {
    BASE: '/api/Tasks',
    BY_ID: (id: string) => `/api/Tasks/${id}`,
    COMMENTS: (taskId: string) => `/api/Tasks/${taskId}/comments`,
    ACTIVITIES: (taskId: string) => `/api/Tasks/${taskId}/activities`,
    TIMELOGS: (taskId: string) => `/api/Tasks/${taskId}/timelogs`,
    ATTACHMENTS: (taskId: string) => `/api/Tasks/${taskId}/attachments`,
  },
  ATTACHMENTS: {
    BY_ID: (id: string) => `/api/Attachments/${id}`,
    DOWNLOAD: (id: string) => `/api/Attachments/${id}/download`,
  },
  PROJECT_TEMPLATES: {
    BASE: '/api/project-templates',
    BY_ID: (id: string) => `/api/project-templates/${id}`,
  },
  TIMELOGS: {
    BY_ID: (id: string) => `/api/Timelogs/${id}`,
  },
  COMMENTS: {
    BASE: '/api/Comments',
    BY_ID: (id: string) => `/api/Comments/${id}`,
  },
  NOTIFICATIONS: {
    BASE: '/api/Notifications',
    UNREAD_COUNT: '/api/Notifications/unread-count',
    BY_ID: (id: string) => `/api/Notifications/${id}`,
    MARK_READ: (id: string) => `/api/Notifications/${id}/read`,
    MARK_ALL_READ: '/api/Notifications/read-all',
  },
} as const;
