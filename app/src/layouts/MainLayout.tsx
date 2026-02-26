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
    <div className="min-h-screen flex flex-col app-bg">
      <Topbar onMenuClick={handleMenuClick} sidebarCollapsed={sidebarCollapsed} />
      <div className="flex flex-1">
        <Sidebar mobileOpen={sidebarOpen} onClose={() => setSidebarOpen(false)} collapsed={sidebarCollapsed} />
        <main
          className={'flex-1 min-w-0 transition-[margin] duration-200 main-content-bg ' + (sidebarCollapsed ? 'lg:ml-16' : 'lg:ml-64')}
        >
          <div className="p-6 lg:p-8 max-w-6xl">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
