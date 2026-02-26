import { NavLink } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const navItems: Array<{ to: string; label: string; icon: string; adminOnly?: boolean }> = [
  { to: '/', label: 'Dashboard', icon: 'M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z' },
  { to: '/profile', label: 'Profile', icon: 'M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z' },
  { to: '/admin-settings', label: 'Admin Settings', icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z', adminOnly: true },
];

interface SidebarProps {
  mobileOpen: boolean;
  onClose: () => void;
  collapsed: boolean;
}

export function Sidebar({ mobileOpen, onClose, collapsed }: SidebarProps) {
  const { user } = useAuth();
  const isAdmin = user?.role != null && user.role.localeCompare('Admin', undefined, { sensitivity: 'accent' }) === 0;
  const items = navItems.filter((item) => !item.adminOnly || isAdmin);

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    'flex items-center gap-3 pl-2 pr-3 py-2 text-sm font-normal rounded transition-colors duration-150 ' +
    (isActive
      ? ' bg-[#e6f4ff] dark:bg-[#004578] text-[#0078d4] dark:text-[#4da9ff] border-l-4 border-[#0078d4] -ml-px'
      : ' text-[#323130] dark:text-[#f3f2f1] hover:bg-[#f3f2f1] dark:hover:bg-[#3b3a39] border-l-4 border-transparent');

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
          {items.map(({ to, label, icon }) => (
            <NavLink key={to} to={to} className={linkClass} onClick={onClose}>
              <svg className="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={icon} />
              </svg>
              {!collapsed && <span>{label}</span>}
            </NavLink>
          ))}
        </nav>
      </aside>
    </>
  );
}
