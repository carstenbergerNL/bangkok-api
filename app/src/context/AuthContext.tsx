import { createContext, useCallback, useContext, useMemo, useState } from 'react';
import type { LoginRequest } from '../models/LoginRequest';
import type { LoginResponse } from '../models/LoginResponse';
import {
  getAccessToken,
  getStoredUser,
  login as apiLogin,
  logout as doLogout,
  setAuthFromLoginResponse,
  type StoredUser,
} from '../services/authService';

interface AuthContextValue {
  user: StoredUser | null;
  token: string | null;
  isAuthenticated: boolean;
  login: (credentials: LoginRequest) => Promise<{ success: boolean; error?: string }>;
  logout: () => void;
  setUser: (user: StoredUser | null) => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUserState] = useState<StoredUser | null>(getStoredUser);
  const token = getAccessToken();

  const setUser = useCallback((u: StoredUser | null) => {
    setUserState(u);
  }, []);

  const login = useCallback(async (credentials: LoginRequest) => {
    try {
      const res = await apiLogin(credentials);
      if (res.success && res.data) {
        setAuthFromLoginResponse(res.data as LoginResponse, credentials.email);
        setUserState({
          email: credentials.email,
          displayName: res.data.displayName,
          roles: res.data.roles ?? [],
          applicationId: res.data.applicationId,
        });
        return { success: true };
      }
      const message = res.error?.message ?? res.message ?? 'Login failed';
      return { success: false, error: message };
    } catch (err: unknown) {
      const axiosErr = err as { response?: { status?: number; data?: { error?: { message?: string }; message?: string } }; message?: string };
      if (axiosErr.response) {
        const msg = axiosErr.response.data?.error?.message ?? axiosErr.response.data?.message ?? `Request failed (${axiosErr.response.status})`;
        return { success: false, error: msg };
      }
      const msg = axiosErr.message ?? 'Login failed';
      if (msg.includes('Network Error') || msg.includes('Failed to fetch')) {
        return { success: false, error: 'Cannot reach the API. Check VITE_API_BASE_URL in .env and that the API is running. If using HTTPS, accept the certificate in your browser first.' };
      }
      return { success: false, error: msg };
    }
  }, []);

  const logout = useCallback(() => {
    doLogout();
    setUserState(null);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token,
      isAuthenticated: !!user && !!getAccessToken(),
      login,
      logout,
      setUser,
    }),
    [user, login, logout, setUser]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
