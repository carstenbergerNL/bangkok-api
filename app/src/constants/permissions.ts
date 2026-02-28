/** Permission names used for feature visibility and access control. */
export const PERMISSIONS = {
  ViewAdminSettings: 'ViewAdminSettings',
} as const;

export type PermissionName = (typeof PERMISSIONS)[keyof typeof PERMISSIONS];
