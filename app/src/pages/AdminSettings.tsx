import { useState } from 'react';
import { AdminSettingsTabs, type AdminSettingsTab } from '../components/AdminSettingsTabs';
import { UsersSection } from './admin-settings/UsersSection';
import { RolesSection } from './admin-settings/RolesSection';
import { PermissionsSection } from './admin-settings/PermissionsSection';

export function AdminSettings() {
  const [activeTab, setActiveTab] = useState<AdminSettingsTab>('users');

  return (
    <div className="space-y-6">
      <div className="page-header">
        <h1>Admin Settings</h1>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          Manage users, roles, and permissions.
        </p>
      </div>

      <AdminSettingsTabs activeTab={activeTab} onTabChange={setActiveTab} />

      <div
        id={`panel-${activeTab}`}
        role="tabpanel"
        aria-labelledby={`tab-${activeTab}`}
        className="rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 shadow-sm overflow-hidden transition-opacity duration-200"
      >
        <div className="p-6">
          {activeTab === 'users' && <UsersSection />}
          {activeTab === 'roles' && <RolesSection />}
          {activeTab === 'permissions' && <PermissionsSection />}
        </div>
      </div>
    </div>
  );
}
