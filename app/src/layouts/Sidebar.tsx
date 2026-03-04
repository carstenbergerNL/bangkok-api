import { NavLink } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { PERMISSIONS } from '../constants/permissions';
import { usePermissions } from '../hooks/usePermissions';
import { useActiveModules } from '../hooks/useActiveModules';

const navItems: Array<{ to: string; label: string; icon: string; permission?: string; moduleKey?: string; role?: string; end?: boolean }> = [
  { to: '/', label: 'Dashboard', icon: 'M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z' },
  { to: '/profile', label: 'Profile', icon: 'M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z' },
  { to: '/billing', label: 'Billing', icon: 'M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z' },
  { to: '/projects', label: 'Projects', icon: 'M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z', moduleKey: 'ProjectManagement', end: false },
  { to: '/tasks', label: 'Tasks', icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4', moduleKey: 'Tasks', end: true },
  { to: '/admin-settings', label: 'Admin Settings', icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z', permission: PERMISSIONS.ViewAdminSettings },
  { to: '/platform-dashboard', label: 'Platform Admin', icon: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m7 4h1m1-4v-4m0 0h-4m-4 0h4m-4 0V9', role: 'SuperAdmin' },
];

interface SidebarProps {
  mobileOpen: boolean;
  onClose: () => void;
  collapsed: boolean;
}

export function Sidebar({ mobileOpen, onClose, collapsed }: SidebarProps) {
  const { hasPermission } = usePermissions();
  const { activeModuleKeys } = useActiveModules();
  const { user } = useAuth();
  const userRoles = (user?.roles ?? []).map((r) => r?.toLowerCase());
  const items = navItems.filter((item) => {
    if (item.permission && !hasPermission(item.permission)) return false;
    if (item.moduleKey && !activeModuleKeys.includes(item.moduleKey)) return false;
    if (item.role && !userRoles.includes(item.role.toLowerCase())) return false;
    return true;
  });

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    'flex items-center gap-3 pl-2 pr-3 py-2 text-sm font-normal rounded transition-colors duration-150 ' +
    (isActive
      ? ' bg-primary-100 dark:bg-primary-900/40 text-primary-600 dark:text-primary-300 border-l-4 border-primary-500 dark:border-primary-400 -ml-px'
      : ' text-gray-900 dark:text-slate-100 hover:bg-gray-100 dark:hover:bg-slate-700/80 border-l-4 border-transparent');

  return (
    <>
      <div
        className={'fixed inset-0 z-40 lg:hidden transition-opacity duration-200 ' + (mobileOpen ? 'opacity-100 bg-black/20' : 'opacity-0 pointer-events-none')}
        onClick={onClose}
        aria-hidden
      />
      <aside
        className={
          'fixed top-12 left-0 z-40 h-[calc(100vh-3rem)] w-64 border-r transition-[width,transform] duration-200 ease-out lg:translate-x-0 ' +
          (mobileOpen ? 'translate-x-0' : '-translate-x-full') +
          (collapsed ? ' lg:w-16' : ' lg:w-64')
        }
        style={{
          backgroundColor: 'var(--sidebar-bg, #faf9f8)',
          borderColor: 'var(--sidebar-border, #edebe9)',
        }}
      >
        <nav className="p-2 flex flex-col gap-0.5">
          {items.map(({ to, label, icon, end }) => (
            <NavLink
              key={to}
              to={to}
              className={linkClass}
              onClick={onClose}
              end={end === undefined ? true : end}
            >
              <svg className="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={icon} />
              </svg>
              {!collapsed && <span className="text-inherit">{label}</span>}
            </NavLink>
          ))}
        </nav>
      </aside>
    </>
  );
}
