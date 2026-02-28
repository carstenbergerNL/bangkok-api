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
  roles: string[];
  /** Permission names. May be missing for users stored before permissions were added. */
  permissions?: string[];
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

/** Get user id from stored user or by decoding the JWT (for sessions created before applicationId was returned). */
export function getCurrentUserId(): string | null {
  const stored = getStoredUser();
  if (stored?.applicationId) return stored.applicationId;
  const token = getAccessToken();
  if (!token) return null;
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    const payload = parts[1];
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + (4 - base64.length % 4) % 4, '=');
    const json = atob(padded);
    const decoded = JSON.parse(json) as Record<string, string>;
    return decoded.sub ?? decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? null;
  } catch {
    return null;
  }
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
    roles: data.roles ?? [],
    permissions: data.permissions ?? [],
    applicationId: data.applicationId,
  };
  localStorage.setItem(AUTH_STORAGE_KEYS.user, JSON.stringify(user));
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export function changePassword(request: ChangePasswordRequest): Promise<ApiResponse<unknown>> {
  return apiClient
    .post<ApiResponse<unknown>>('/api/Auth/change-password', request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function initAuthUnauthorizedHandler(redirectToLogin: () => void): void {
  setOnUnauthorized(() => {
    logout();
    redirectToLogin();
  });
}
