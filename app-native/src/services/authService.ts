/**
 * Auth service – mirrors app (web) API usage. Token storage to be wired to SecureStore.
 */
import { API_PATHS } from '../constants';
import type { ApiResponse, LoginRequest, LoginResponse } from '../models';
import { apiRequest, setAccessToken } from '../api/client';

export function login(credentials: LoginRequest): Promise<ApiResponse<LoginResponse>> {
  return apiRequest<ApiResponse<LoginResponse>>(API_PATHS.AUTH.LOGIN, {
    method: 'POST',
    body: credentials,
  });
}

export function setAuthToken(token: string | null): void {
  setAccessToken(token);
}
