import { useState } from 'react';
import { AdminSettingsTabs, type AdminSettingsTab } from '../components/AdminSettingsTabs';
import { UsersSection } from './admin-settings/UsersSection';
import { UserManagementSection } from './admin-settings/UserManagementSection';
import { RolesSection } from './admin-settings/RolesSection';
import { PermissionsSection } from './admin-settings/PermissionsSection';
import { TemplatesSection } from './admin-settings/TemplatesSection';
import { ModulesSection } from './admin-settings/ModulesSection';
import { ModuleAccessSection } from './admin-settings/ModuleAccessSection';

export function AdminSettings() {
  const [activeTab, setActiveTab] = useState<AdminSettingsTab>('users');

  return (
    <div className="space-y-6">
      <div className="page-header">
        <h1>Admin Settings</h1>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          Manage users, roles, permissions, project templates, and tenant modules.
        </p>
      </div>

      <AdminSettingsTabs activeTab={activeTab} onTabChange={setActiveTab} />

      <div
        id={`panel-${activeTab}`}
        role="tabpanel"
        aria-labelledby={`tab-${activeTab}`}
        className="rounded-xl border border-gray-200 dark:border-[#2d3d5c] bg-white dark:bg-[#1e2a4a] shadow-sm overflow-hidden transition-opacity duration-200"
      >
        <div className="p-6">
          {activeTab === 'users' && <UsersSection />}
          {activeTab === 'usersAndAccess' && <UserManagementSection />}
          {activeTab === 'roles' && <RolesSection />}
          {activeTab === 'permissions' && <PermissionsSection />}
          {activeTab === 'templates' && <TemplatesSection />}
          {activeTab === 'tenant' && <ModulesSection />}
          {activeTab === 'moduleAccess' && <ModuleAccessSection />}
        </div>
      </div>
    </div>
  );
}
