import { apiClient, setOnUnauthorized } from '../api/client';
import type { ApiResponse } from '../models/ApiResponse';
import type { LoginRequest } from '../models/LoginRequest';
import type { LoginResponse } from '../models/LoginResponse';

const AUTH_STORAGE_KEYS = {
  accessToken: 'accessToken',
  refreshToken: 'refreshToken',
  user: 'user',
} as const;

export interface StoredUser {
  email: string;
  displayName?: string;
  role?: string;
  applicationId?: string;
}

export function getStoredUser(): StoredUser | null {
  try {
    const raw = localStorage.getItem(AUTH_STORAGE_KEYS.user);
    return raw ? (JSON.parse(raw) as StoredUser) : null;
  } catch {
    return null;
  }
}

export function getAccessToken(): string | null {
  return localStorage.getItem(AUTH_STORAGE_KEYS.accessToken);
}

export function login(credentials: LoginRequest): Promise<ApiResponse<LoginResponse>> {
  return apiClient.post<ApiResponse<LoginResponse>>('/api/Auth/login', credentials).then((res) => res.data);
}

export function logout(): void {
  localStorage.removeItem(AUTH_STORAGE_KEYS.accessToken);
  localStorage.removeItem(AUTH_STORAGE_KEYS.refreshToken);
  localStorage.removeItem(AUTH_STORAGE_KEYS.user);
}

export function setAuthFromLoginResponse(data: LoginResponse, email?: string): void {
  if (data.accessToken) localStorage.setItem(AUTH_STORAGE_KEYS.accessToken, data.accessToken);
  if (data.refreshToken) localStorage.setItem(AUTH_STORAGE_KEYS.refreshToken, data.refreshToken);
  const user: StoredUser = {
    email: email ?? '',
    displayName: data.displayName ?? undefined,
    role: data.role,
    applicationId: data.applicationId,
  };
  localStorage.setItem(AUTH_STORAGE_KEYS.user, JSON.stringify(user));
}

export function initAuthUnauthorizedHandler(redirectToLogin: () => void): void {
  setOnUnauthorized(() => {
    logout();
    redirectToLogin();
  });
}
