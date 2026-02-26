import { useState, useRef, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useDarkMode } from '../hooks/useDarkMode';
import { getCurrentUserId } from '../services/authService';
import { getProfileByUserId } from '../services/profileService';

interface TopbarProps {
  onMenuClick?: () => void;
}

export function Topbar({ onMenuClick }: TopbarProps) {
  const { user, logout } = useAuth();
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
    const onProfileUpdated = () => loadAvatar();
    window.addEventListener('profile-updated', onProfileUpdated);
    return () => window.removeEventListener('profile-updated', onProfileUpdated);
  }, []);

  const displayName = user?.displayName || user?.email || 'User';
  const initial = displayName.charAt(0).toUpperCase();

  return (
    <header className="h-14 flex-shrink-0 flex items-center justify-between px-4 lg:px-6 bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 shadow-sm transition-colors duration-200">
      <div className="flex items-center gap-2">
        {onMenuClick && (
          <button
            type="button"
            onClick={onMenuClick}
            className="btn-icon"
            aria-label="Toggle menu"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>
        )}
        <Link to="/" className="text-lg font-semibold text-gray-900 dark:text-white tracking-tight px-1">
          Bangkok
        </Link>
      </div>
      <div className="flex items-center gap-1">
        <button type="button" onClick={toggleDark} className="btn-icon" aria-label={isDark ? 'Light mode' : 'Dark mode'}>
          {isDark ? (
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z" /></svg>
          ) : (
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" /></svg>
          )}
        </button>
        <div className="relative" ref={ref}>
          <button type="button" onClick={() => setOpen((o) => !o)} className="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors duration-200">
            {avatarSrc ? (
              <img src={avatarSrc} alt="" className="w-8 h-8 rounded-full object-cover border border-gray-200 dark:border-gray-700" />
            ) : (
              <span className="w-8 h-8 rounded-full bg-primary-100 dark:bg-primary-900/40 flex items-center justify-center text-primary-600 dark:text-primary-400 text-sm font-medium">{initial}</span>
            )}
            <span className="text-sm font-medium text-gray-700 dark:text-gray-300 hidden sm:inline max-w-[140px] truncate">{displayName}</span>
            <svg className="w-4 h-4 text-gray-400 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" /></svg>
          </button>
          {open && (
            <div className="absolute right-0 mt-2 w-52 py-1 bg-white dark:bg-gray-900 rounded-card border border-gray-200 dark:border-gray-700 shadow-dropdown z-50">
              <div className="px-4 py-3 border-b border-gray-100 dark:border-gray-800">
                <p className="text-xs text-gray-500 dark:text-gray-400">Signed in as</p>
                <p className="text-sm font-medium text-gray-900 dark:text-white truncate mt-0.5">{user?.email || '—'}</p>
              </div>
              <Link to="/" className="block px-4 py-2.5 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors duration-150" onClick={() => setOpen(false)}>Dashboard</Link>
              <button type="button" className="w-full text-left px-4 py-2.5 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors duration-150" onClick={() => { setOpen(false); logout(); }}>Log out</button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
