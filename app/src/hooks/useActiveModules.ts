import { useCallback, useEffect, useState } from 'react';
import { getActiveModules } from '../services/tenantModuleService';

/**
 * Returns the list of active module keys for the current tenant.
 * Used by Sidebar to show only enabled modules. Refetch after login/tenant change.
 */
export function useActiveModules(): { activeModuleKeys: string[]; loading: boolean; refetch: () => void } {
  const [activeModuleKeys, setActiveModuleKeys] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);

  const refetch = useCallback(() => {
    setLoading(true);
    getActiveModules()
      .then((res) => {
        const payload = res.data ?? (res as { data?: { activeModuleKeys?: string[] } }).data;
        const keys = payload?.activeModuleKeys ?? [];
        setActiveModuleKeys(Array.isArray(keys) ? keys : []);
      })
      .catch(() => setActiveModuleKeys([]))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    refetch();
  }, [refetch]);

  return { activeModuleKeys, loading, refetch };
}
