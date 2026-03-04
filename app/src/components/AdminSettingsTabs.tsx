export type AdminSettingsTab = 'users' | 'roles' | 'permissions' | 'templates' | 'tenant' | 'moduleAccess' | 'usersAndAccess';

interface AdminSettingsTabsProps {
  activeTab: AdminSettingsTab;
  onTabChange: (tab: AdminSettingsTab) => void;
}

const TABS: { id: AdminSettingsTab; label: string }[] = [
  { id: 'users', label: 'Users' },
  { id: 'usersAndAccess', label: 'Users & Access' },
  { id: 'roles', label: 'Roles' },
  { id: 'permissions', label: 'Permissions' },
  { id: 'templates', label: 'Templates' },
  { id: 'tenant', label: 'Tenant' },
  { id: 'moduleAccess', label: 'Module Access' },
];

export function AdminSettingsTabs({ activeTab, onTabChange }: AdminSettingsTabsProps) {
  return (
    <nav className="border-b border-gray-200 dark:border-[#2d3d5c]" aria-label="Admin settings sections">
      <div className="flex gap-1">
        {TABS.map((tab) => {
          const isActive = activeTab === tab.id;
          return (
            <button
              key={tab.id}
              type="button"
              role="tab"
              aria-selected={isActive}
              aria-controls={`panel-${tab.id}`}
              id={`tab-${tab.id}`}
              onClick={() => onTabChange(tab.id)}
              className={`
                relative px-4 py-3 text-sm font-medium transition-colors duration-200
                focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-blue-500
                ${isActive
                  ? 'text-blue-600 dark:text-blue-400'
                  : 'text-gray-600 dark:text-slate-400 hover:text-gray-900 dark:hover:text-slate-100'
                }
              `}
            >
              {tab.label}
              {isActive && (
                <span
                  className="absolute bottom-0 left-0 right-0 h-0.5 bg-blue-600 dark:bg-blue-400 rounded-t transition-all duration-200"
                  aria-hidden
                />
              )}
            </button>
          );
        })}
      </div>
    </nav>
  );
}
