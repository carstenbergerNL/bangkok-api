import { useState, useCallback } from 'react';
import { Outlet } from 'react-router-dom';
import { Topbar } from './Topbar';
import { Sidebar } from './Sidebar';

export function MainLayout() {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  const handleMenuClick = useCallback(() => {
    if (typeof window !== 'undefined' && window.innerWidth >= 1024) {
      setSidebarCollapsed((c) => !c);
    } else {
      setSidebarOpen(true);
    }
  }, []);

  return (
    <div className="min-h-screen flex flex-col bg-main-area">
      <Topbar onMenuClick={handleMenuClick} />
      <div className="flex flex-1">
        <Sidebar mobileOpen={sidebarOpen} onClose={() => setSidebarOpen(false)} collapsed={sidebarCollapsed} />
        <main className={'flex-1 min-w-0 transition-[margin] duration-200 ' + (sidebarCollapsed ? 'lg:ml-20' : 'lg:ml-64')}>
          <div className="p-4 md:p-6 lg:p-8">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
