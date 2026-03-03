import { useCallback, useEffect, useState } from 'react';
import { getTenantModulesManagement, setModuleActive, type TenantModuleListItem } from '../../services/tenantModuleService';
import { addToast } from '../../utils/toast';

export function ModulesSection() {
  const [modules, setModules] = useState<TenantModuleListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [togglingKey, setTogglingKey] = useState<string | null>(null);

  const load = useCallback(() => {
    setLoading(true);
    setError(null);
    getTenantModulesManagement()
      .then((res) => {
        const data = res.data ?? (res as unknown as { data?: TenantModuleListItem[] }).data;
        const list = Array.isArray(data) ? data : [];
        setModules(list);
      })
      .catch(() => {
        setError('Failed to load modules.');
        addToast('error', 'Failed to load tenant modules.');
      })
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const handleToggle = useCallback((item: TenantModuleListItem) => {
    const newActive = !item.isActive;
    setTogglingKey(item.key);
    setModuleActive(item.key, newActive)
      .then(() => {
        setModules((prev) => prev.map((m) => (m.key === item.key ? { ...m, isActive: newActive } : m)));
        addToast('success', `${item.name} is now ${newActive ? 'enabled' : 'disabled'}.`);
      })
      .catch(() => {
        addToast('error', `Failed to update ${item.name}.`);
      })
      .finally(() => setTogglingKey(null));
  }, []);

  if (loading && modules.length === 0) {
    return (
      <div className="animate-pulse space-y-4">
        <div className="h-6 w-48 bg-gray-200 dark:bg-gray-700 rounded" />
        <div className="h-12 bg-gray-200 dark:bg-gray-700 rounded" />
        <div className="h-12 bg-gray-200 dark:bg-gray-700 rounded" />
        <div className="h-12 bg-gray-200 dark:bg-gray-700 rounded" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold text-gray-900 dark:text-white">Manage Modules</h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          Enable or disable modules for your organization. Only active modules appear in the sidebar and are accessible to users.
        </p>
      </div>

      {error && <div className="alert-error">{error}</div>}

      <div className="rounded-lg border border-gray-200 dark:border-[#2d3d5c] overflow-hidden bg-gray-50/50 dark:bg-blue-900/20">
        {modules.length === 0 && !loading ? (
          <div className="p-12 text-center text-gray-500 dark:text-gray-400 text-sm">
            No modules available. Run database migrations to seed modules.
          </div>
        ) : (
          <ul className="divide-y divide-gray-200 dark:divide-[#2d3d5c]">
            {modules.map((m) => (
              <li key={m.key} className="flex items-center justify-between gap-4 px-4 py-3 hover:bg-gray-100/80 dark:hover:bg-[#2d3d5c]/50 transition-colors">
                <div className="min-w-0">
                  <p className="font-medium text-gray-900 dark:text-white">{m.name}</p>
                  {m.description && (
                    <p className="text-sm text-gray-500 dark:text-gray-400 truncate">{m.description}</p>
                  )}
                </div>
                <label className="flex items-center gap-2 shrink-0 cursor-pointer">
                  <span className="text-sm text-gray-600 dark:text-gray-300">
                    {m.isActive ? 'On' : 'Off'}
                  </span>
                  <button
                    type="button"
                    role="switch"
                    aria-checked={m.isActive}
                    disabled={togglingKey === m.key}
                    onClick={() => handleToggle(m)}
                    className={`
                      relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent
                      transition-colors duration-200 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:ring-offset-2
                      disabled:opacity-50 disabled:cursor-not-allowed
                      ${m.isActive ? 'bg-blue-600 dark:bg-blue-500' : 'bg-gray-200 dark:bg-gray-600'}
                    `}
                  >
                    <span
                      className={`
                        pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200
                        ${m.isActive ? 'translate-x-5' : 'translate-x-1'}
                      `}
                    />
                  </button>
                </label>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
