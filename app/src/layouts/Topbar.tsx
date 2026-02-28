import { useState, useRef, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

function useIsAdmin() {
  const { user } = useAuth();
  const roles = user?.roles ?? [];
  return roles.some((r) => r?.localeCompare('Admin', undefined, { sensitivity: 'accent' }) === 0);
}
import { useDarkMode } from '../hooks/useDarkMode';
import { getCurrentUserId } from '../services/authService';
import { getProfileByUserId } from '../services/profileService';

interface TopbarProps {
  onMenuClick?: () => void;
  sidebarCollapsed?: boolean;
}

export function Topbar({ onMenuClick, sidebarCollapsed }: TopbarProps) {
  const { user, logout } = useAuth();
  const isAdmin = useIsAdmin();
  const rolesDisplay = (user?.roles ?? []).filter(Boolean).join(', ') || '—';
  const [isDark, toggleDark] = useDarkMode();
  const [open, setOpen] = useState(false);
  const [avatarSrc, setAvatarSrc] = useState<string | null>(null);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    }
    document.addEventListener('click', handleClickOutside);
    return () => document.removeEventListener('click', handleClickOutside);
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

  return (
    <header
      className="h-12 flex-shrink-0 flex items-center justify-between transition-colors duration-200"
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
          Bangkok
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
              {isAdmin && (
                <Link
                  to="/roles"
                  className="block px-4 py-2.5 text-sm transition-colors duration-150 hover:bg-[#f3f2f1] dark:hover:bg-[#3b3a39]"
                  style={{ color: 'var(--dropdown-text, #323130)' }}
                  onClick={() => setOpen(false)}
                >
                  Roles
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
