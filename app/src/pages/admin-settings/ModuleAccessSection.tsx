import { useCallback, useEffect, useState } from 'react';
import {
  getTenantModulesManagement,
  getModuleUsers,
  grantModuleAccess,
  revokeModuleAccess,
  type TenantModuleListItem,
  type ModuleAccessUser,
} from '../../services/tenantModuleService';
import { addToast } from '../../utils/toast';

export function ModuleAccessSection() {
  const [modules, setModules] = useState<TenantModuleListItem[]>([]);
  const [selectedModuleKey, setSelectedModuleKey] = useState<string | null>(null);
  const [users, setUsers] = useState<ModuleAccessUser[]>([]);
  const [loadingModules, setLoadingModules] = useState(true);
  const [loadingUsers, setLoadingUsers] = useState(false);
  const [saving, setSaving] = useState(false);
  const [pending, setPending] = useState<Record<string, boolean>>({});

  const activeModules = modules.filter((m) => m.isActive);

  const loadModules = useCallback(() => {
    setLoadingModules(true);
    getTenantModulesManagement()
      .then((res) => {
        const data = res.data ?? (res as unknown as { data?: TenantModuleListItem[] }).data;
        setModules(Array.isArray(data) ? data : []);
      })
      .catch(() => addToast('error', 'Failed to load modules.'))
      .finally(() => setLoadingModules(false));
  }, []);

  useEffect(() => {
    loadModules();
  }, [loadModules]);

  useEffect(() => {
    if (!selectedModuleKey) {
      setUsers([]);
      setPending({});
      return;
    }
    setLoadingUsers(true);
    setPending({});
    getModuleUsers(selectedModuleKey)
      .then((res) => {
        const data = res.data ?? (res as unknown as { data?: ModuleAccessUser[] }).data;
        setUsers(Array.isArray(data) ? data : []);
      })
      .catch(() => addToast('error', 'Failed to load module access.'))
      .finally(() => setLoadingUsers(false));
  }, [selectedModuleKey]);

  const getEffectiveAccess = useCallback(
    (u: ModuleAccessUser) => (pending[u.userId] !== undefined ? pending[u.userId] : u.hasAccess),
    [pending]
  );

  const handleToggle = useCallback((userId: string, current: boolean) => {
    setPending((prev) => ({ ...prev, [userId]: !current }));
  }, []);

  const hasChanges = Object.keys(pending).length > 0;

  const handleSave = useCallback(() => {
    if (!selectedModuleKey || !hasChanges) return;
    setSaving(true);
    const promises: Promise<void>[] = [];
    users.forEach((u) => {
      const desired = pending[u.userId] !== undefined ? pending[u.userId] : u.hasAccess;
      if (desired && !u.hasAccess) promises.push(grantModuleAccess(selectedModuleKey, u.userId));
      if (!desired && u.hasAccess) promises.push(revokeModuleAccess(selectedModuleKey, u.userId));
    });
    Promise.all(promises)
      .then(() => {
        setPending({});
        addToast('success', 'Module access updated.');
        return getModuleUsers(selectedModuleKey);
      })
      .then((res) => {
        const data = res.data ?? (res as unknown as { data?: ModuleAccessUser[] }).data;
        setUsers(Array.isArray(data) ? data : []);
      })
      .catch(() => addToast('error', 'Failed to save module access.'))
      .finally(() => setSaving(false));
  }, [selectedModuleKey, hasChanges, users, pending]);

  if (loadingModules) {
    return (
      <div className="animate-pulse space-y-4">
        <div className="h-6 w-48 bg-gray-200 dark:bg-gray-700 rounded" />
        <div className="h-10 bg-gray-200 dark:bg-gray-700 rounded" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold text-gray-900 dark:text-white">Module Access</h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          Grant or revoke access to modules for users. Only active modules are listed. Only users in your organization are shown.
        </p>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Select module</label>
        <select
          value={selectedModuleKey ?? ''}
          onChange={(e) => setSelectedModuleKey(e.target.value || null)}
          className="block w-full max-w-xs rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white shadow-sm focus:ring-blue-500 focus:border-blue-500"
        >
          <option value="">— Select module —</option>
          {activeModules.map((m) => (
            <option key={m.key} value={m.key}>
              {m.name}
            </option>
          ))}
        </select>
      </div>

      {selectedModuleKey && (
        <>
          {loadingUsers ? (
            <div className="animate-pulse space-y-2">
              {[1, 2, 3, 4, 5].map((i) => (
                <div key={i} className="h-12 bg-gray-200 dark:bg-gray-700 rounded" />
              ))}
            </div>
          ) : (
            <div className="rounded-lg border border-gray-200 dark:border-[#2d3d5c] overflow-hidden bg-gray-50/50 dark:bg-blue-900/20">
              {users.length === 0 ? (
                <div className="p-8 text-center text-gray-500 dark:text-gray-400 text-sm">No users in this organization.</div>
              ) : (
                <ul className="divide-y divide-gray-200 dark:divide-[#2d3d5c]">
                  {users.map((u) => {
                    const hasAccess = getEffectiveAccess(u);
                    return (
                      <li
                        key={u.userId}
                        className="flex items-center justify-between gap-4 px-4 py-3 hover:bg-gray-100/80 dark:hover:bg-[#2d3d5c]/50"
                      >
                        <div className="min-w-0">
                          <p className="font-medium text-gray-900 dark:text-white">{u.displayName || u.email || u.userId}</p>
                          {u.email && u.displayName && (
                            <p className="text-sm text-gray-500 dark:text-gray-400 truncate">{u.email}</p>
                          )}
                        </div>
                        <label className="flex items-center gap-2 shrink-0 cursor-pointer">
                          <span className="text-sm text-gray-600 dark:text-gray-300">
                            {hasAccess ? 'Has access' : 'No access'}
                          </span>
                          <input
                            type="checkbox"
                            checked={hasAccess}
                            onChange={() => handleToggle(u.userId, hasAccess)}
                            className="h-4 w-4 rounded border-gray-300 dark:border-gray-600 text-blue-600 focus:ring-blue-500"
                          />
                        </label>
                      </li>
                    );
                  })}
                </ul>
              )}
            </div>
          )}

          {hasChanges && (
            <div className="flex justify-end">
              <button
                type="button"
                disabled={saving}
                onClick={handleSave}
                className="px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white text-sm font-medium"
              >
                {saving ? 'Saving…' : 'Save changes'}
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
