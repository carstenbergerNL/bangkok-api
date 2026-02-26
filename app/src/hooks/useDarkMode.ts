import { useCallback, useEffect, useState } from 'react';

const STORAGE_KEY = 'bangkok-dark-mode';

function getInitial(): boolean {
  if (typeof document === 'undefined') return false;
  const stored = localStorage.getItem(STORAGE_KEY);
  if (stored !== null) return stored === 'true';
  return window.matchMedia('(prefers-color-scheme: dark)').matches;
}

function applyDarkMode(isDark: boolean) {
  if (isDark) {
    document.documentElement.classList.add('dark');
    localStorage.setItem(STORAGE_KEY, 'true');
  } else {
    document.documentElement.classList.remove('dark');
    localStorage.setItem(STORAGE_KEY, 'false');
  }
}

export function useDarkMode(): [boolean, () => void] {
  const [isDark, setIsDark] = useState(getInitial);

  useEffect(() => {
    applyDarkMode(isDark);
  }, [isDark]);

  const toggle = useCallback(() => {
    setIsDark((prev) => !prev);
  }, []);

  return [isDark, toggle];
}
