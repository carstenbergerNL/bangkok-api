import { useState, useRef, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { PERMISSIONS } from '../constants/permissions';
import { useAuth } from '../context/AuthContext';
import { useDarkMode } from '../hooks/useDarkMode';
import { usePermissions } from '../hooks/usePermissions';
import { getCurrentUserId } from '../services/authService';
import { getProfileByUserId } from '../services/profileService';
import {
  getNotifications,
  getUnreadCount,
  markNotificationRead,
  markAllNotificationsRead,
  type Notification,
} from '../services/notificationService';

interface TopbarProps {
  onMenuClick?: () => void;
  sidebarCollapsed?: boolean;
}

export function Topbar({ onMenuClick, sidebarCollapsed }: TopbarProps) {
  const { user, logout } = useAuth();
  const { hasPermission } = usePermissions();
  const canViewAdmin = hasPermission(PERMISSIONS.ViewAdminSettings);
  const rolesDisplay = (user?.roles ?? []).filter(Boolean).join(', ') || '—';
  const [isDark, toggleDark] = useDarkMode();
  const [open, setOpen] = useState(false);
  const [avatarSrc, setAvatarSrc] = useState<string | null>(null);
  const ref = useRef<HTMLDivElement>(null);

  const [notificationOpen, setNotificationOpen] = useState(false);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [notificationsLoading, setNotificationsLoading] = useState(false);
  const notificationRef = useRef<HTMLDivElement>(null);

  const loadNotifications = useCallback(() => {
    getNotifications().then((res) => {
      const data = res.data ?? (res as { Data?: Notification[] }).Data;
      setNotifications(Array.isArray(data) ? data : []);
    });
    getUnreadCount().then((res) => {
      const count = res.data ?? (res as { Data?: number }).Data;
      setUnreadCount(typeof count === 'number' ? count : 0);
    });
  }, []);

  useEffect(() => {
    loadNotifications();
  }, [loadNotifications]);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
      if (notificationRef.current && !notificationRef.current.contains(e.target as Node)) setNotificationOpen(false);
    }
    document.addEventListener('click', handleClickOutside);
    return () => document.removeEventListener('click', handleClickOutside);
  }, []);

  useEffect(() => {
    if (notificationOpen) {
      setNotificationsLoading(true);
      getNotifications().then((res) => {
        const data = res.data ?? (res as { Data?: Notification[] }).Data;
        setNotifications(Array.isArray(data) ? data : []);
      }).finally(() => setNotificationsLoading(false));
    }
  }, [notificationOpen]);

  const handleMarkRead = useCallback((id: string) => {
    markNotificationRead(id).then((res) => {
      if (res.success) {
        setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)));
        setUnreadCount((c) => Math.max(0, c - 1));
      }
    });
  }, []);

  const handleMarkAllRead = useCallback(() => {
    markAllNotificationsRead().then((res) => {
      if (res.success) {
        setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
        setUnreadCount(0);
      }
    });
  }, []);

  const loadAvatar = () => {
    const userId = getCurrentUserId();
    if (!userId) return;
    getProfileByUserId(userId)
      .then((res) => {
        if (res.success && res.data?.avatarBase64) {
          setAvatarSrc(`data:image/jpeg;base64,${res.data.avatarBase64}`);
        } else {
          setAvatarSrc(null);
        }
      })
      .catch(() => setAvatarSrc(null));
  };

  useEffect(() => {
    loadAvatar();
    const onProfileUpdated = (e: Event) => {
      const customEvent = e as CustomEvent<{ avatarBase64?: string | null } | undefined>;
      const avatarBase64 = customEvent.detail?.avatarBase64;
      if (avatarBase64 != null && avatarBase64 !== '') {
        setAvatarSrc(`data:image/jpeg;base64,${avatarBase64}`);
      } else if (avatarBase64 !== undefined) {
        setAvatarSrc(null);
      } else {
        loadAvatar();
      }
    };
    window.addEventListener('profile-updated', onProfileUpdated);
    return () => window.removeEventListener('profile-updated', onProfileUpdated);
  }, []);

  const displayName = user?.displayName || user?.email || 'User';
  const initial = displayName.charAt(0).toUpperCase();

  function tryFormatDate(iso: string): string {
    try {
      const d = new Date(iso);
      const now = new Date();
      const diffMs = now.getTime() - d.getTime();
      if (diffMs < 60000) return 'Just now';
      if (diffMs < 3600000) return `${Math.floor(diffMs / 60000)}m ago`;
      if (diffMs < 86400000) return `${Math.floor(diffMs / 3600000)}h ago`;
      return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: d.getFullYear() !== now.getFullYear() ? 'numeric' : undefined });
    } catch {
      return '';
    }
  }

  return (
    <header
      className="sticky top-0 z-40 h-12 flex-shrink-0 flex items-center justify-between transition-colors duration-200"
      style={{
        backgroundColor: 'var(--topbar-bg, #ffffff)',
        borderBottom: '1px solid var(--topbar-border, #edebe9)',
      }}
    >
      <div className={`flex items-center flex-1 min-w-0 lg:flex-initial p-2 ${sidebarCollapsed ? 'lg:w-16' : 'lg:w-64'} transition-[width] duration-200`}>
        {onMenuClick && (
          <button
            type="button"
            onClick={onMenuClick}
            className="flex items-center justify-center p-2 lg:px-3 lg:py-2 rounded transition-colors duration-150 hover:bg-[#f3f2f1] dark:hover:bg-[#3b3a39] focus:outline-none focus:ring-2 focus:ring-[#0078d4] focus:ring-offset-2 shrink-0"
            style={{ color: 'var(--topbar-text, #605e5c)' }}
            aria-label="Toggle menu"
          >
            <svg className="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>
        )}
        <Link
          to="/"
          className={`text-base font-semibold px-2 truncate transition-colors duration-150 block ${sidebarCollapsed ? 'lg:hidden' : 'lg:block'}`}
          style={{ color: 'var(--topbar-text, #323130)' }}
        >
          Multi Platform Center
        </Link>
      </div>
      <div className="flex items-center gap-1 px-4 lg:px-6 shrink-0">
        <button type="button" onClick={toggleDark} className="btn-icon" aria-label={isDark ? 'Light mode' : 'Dark mode'}>
          {isDark ? (
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z" /></svg>
          ) : (
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" /></svg>
          )}
        </button>
        <div className="relative" ref={notificationRef}>
          <button
            type="button"
            onClick={() => setNotificationOpen((o) => !o)}
            className="relative flex items-center justify-center p-2 rounded transition-colors duration-150 hover:bg-[#f3f2f1] dark:hover:bg-[#3b3a39] focus:outline-none focus:ring-2 focus:ring-[#0078d4] focus:ring-offset-2"
            style={{ color: 'var(--topbar-text, #323130)' }}
            aria-label="Notifications"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
            </svg>
            {unreadCount > 0 && (
              <span className="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] px-1 flex items-center justify-center text-[10px] font-semibold text-white bg-red-500 rounded-full">
                {unreadCount > 99 ? '99+' : unreadCount}
              </span>
            )}
          </button>
          {notificationOpen && (
            <div
              className="absolute right-0 mt-1 w-[360px] max-h-[400px] flex flex-col z-50 rounded-xl shadow-lg border border-gray-200 dark:border-slate-600 overflow-hidden transition-all duration-200 ease-out"
              style={{
                backgroundColor: 'var(--dropdown-bg, #ffffff)',
              }}
            >
              <div className="flex items-center justify-between px-4 py-3 border-b border-gray-100 dark:border-slate-700">
                <h3 className="text-sm font-semibold" style={{ color: 'var(--dropdown-text, #323130)' }}>Notifications</h3>
                {unreadCount > 0 && (
                  <button
                    type="button"
                    onClick={handleMarkAllRead}
                    className="text-xs font-medium text-blue-600 dark:text-blue-400 hover:underline"
                  >
                    Mark all as read
                  </button>
                )}
              </div>
              <div className="overflow-y-auto flex-1 max-h-[320px]">
                {notificationsLoading ? (
                  <div className="px-4 py-8 text-center text-sm text-gray-500 dark:text-slate-400">Loading…</div>
                ) : notifications.length === 0 ? (
                  <div className="px-4 py-8 text-center text-sm text-gray-500 dark:text-slate-400">No notifications yet.</div>
                ) : (
                  <ul className="py-1">
                    {notifications.map((n) => (
                      <li
                        key={n.id}
                        className={`px-4 py-3 border-b border-gray-50 dark:border-slate-700/50 last:border-b-0 transition-colors ${!n.isRead ? 'bg-blue-50/50 dark:bg-blue-900/10' : 'hover:bg-gray-50 dark:hover:bg-slate-800/50'}`}
                      >
                        <div className="flex items-start justify-between gap-2">
                          <div className="min-w-0 flex-1">
                            <p className="text-sm font-medium truncate" style={{ color: 'var(--dropdown-text, #323130)' }}>{n.title}</p>
                            <p className="text-xs mt-0.5 line-clamp-2 text-gray-600 dark:text-slate-400">{n.message}</p>
                            <p className="text-[10px] mt-1 text-gray-400 dark:text-slate-500">
                              {tryFormatDate(n.createdAt)}
                            </p>
                          </div>
                          {!n.isRead && (
                            <button
                              type="button"
                              onClick={() => handleMarkRead(n.id)}
                              className="shrink-0 text-xs text-blue-600 dark:text-blue-400 hover:underline"
                            >
                              Mark read
                            </button>
                          )}
                        </div>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </div>
          )}
        </div>
        <div className="relative" ref={ref}>
          <button
            type="button"
            onClick={() => setOpen((o) => !o)}
            className="flex items-center gap-2 px-2 py-1.5 rounded transition-colors duration-150"
            style={{ color: 'var(--topbar-text, #323130)' }}
          >
            {avatarSrc ? (
              <img src={avatarSrc} alt="" className="w-8 h-8 rounded-full object-cover border border-[#edebe9] dark:border-[#3b3a39]" />
            ) : (
              <span
                className="w-8 h-8 rounded-full flex items-center justify-center text-sm font-semibold"
                style={{ backgroundColor: '#e6f4ff', color: '#0078d4' }}
              >
                {initial}
              </span>
            )}
            <span className="text-sm font-normal hidden sm:inline max-w-[140px] truncate">{displayName}</span>
            <svg className="w-4 h-4 shrink-0 opacity-80" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" /></svg>
          </button>
          {open && (
            <div
              className="absolute right-0 mt-1 w-56 py-1 z-50 rounded shadow-dropdown"
              style={{
                backgroundColor: 'var(--dropdown-bg, #ffffff)',
                border: '1px solid var(--dropdown-border, #edebe9)',
              }}
            >
              <div className="px-4 py-3 border-b border-[#edebe9] dark:border-[#3b3a39]">
                <p className="text-xs opacity-80">Signed in as</p>
                <p className="text-sm font-normal truncate mt-0.5" style={{ color: 'var(--dropdown-text, #323130)' }}>{user?.email || '—'}</p>
                <p className="text-xs opacity-80 mt-1">Roles: {rolesDisplay}</p>
              </div>
              <Link
                to="/"
                className="block px-4 py-2.5 text-sm transition-colors duration-150 hover:bg-[#f3f2f1] dark:hover:bg-[#3b3a39]"
                style={{ color: 'var(--dropdown-text, #323130)' }}
                onClick={() => setOpen(false)}
              >
                Dashboard
              </Link>
              <Link
                to="/profile"
                className="block px-4 py-2.5 text-sm transition-colors duration-150 hover:bg-[#f3f2f1] dark:hover:bg-[#3b3a39]"
                style={{ color: 'var(--dropdown-text, #323130)' }}
                onClick={() => setOpen(false)}
              >
                Profile
              </Link>
              {canViewAdmin && (
                <Link
                  to="/admin-settings"
                  className="block px-4 py-2.5 text-sm transition-colors duration-150 hover:bg-[#f3f2f1] dark:hover:bg-[#3b3a39]"
                  style={{ color: 'var(--dropdown-text, #323130)' }}
                  onClick={() => setOpen(false)}
                >
                  Admin Settings
                </Link>
              )}
              <button
                type="button"
                className="w-full text-left px-4 py-2.5 text-sm transition-colors duration-150 hover:bg-[#f3f2f1] dark:hover:bg-[#3b3a39]"
                style={{ color: 'var(--dropdown-text, #323130)' }}
                onClick={() => { setOpen(false); logout(); }}
              >
                Sign out
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
