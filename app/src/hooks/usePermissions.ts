import { useMemo } from 'react';
import { useAuth } from '../context/AuthContext';

/** Permissions from the current user (from auth). Use throughout the app for permission-based UI and access. */
export function usePermissions() {
  const { user } = useAuth();
  const permissions = user?.permissions ?? [];

  return useMemo(
    () => ({
      permissions,
      hasPermission: (name: string) =>
        permissions.some((p) => p.localeCompare(name, undefined, { sensitivity: 'accent' }) === 0),
    }),
    [permissions]
  );
}
