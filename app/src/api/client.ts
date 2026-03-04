import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';

const AUTH_REFRESH_URL = '/api/Auth/refresh';
const REFRESH_TOKEN_KEY = 'refreshToken';
const ACCESS_TOKEN_KEY = 'accessToken';

// In dev, use empty baseURL so Vite proxy forwards /api to the API (avoids CORS and HTTPS cert issues)
const baseURL = import.meta.env.DEV ? '' : (import.meta.env.VITE_API_BASE_URL ?? '');

export const apiClient = axios.create({
  baseURL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export type OnUnauthorized = () => void;

let onUnauthorized: OnUnauthorized = () => {};

export function setOnUnauthorized(handler: OnUnauthorized) {
  onUnauthorized = handler;
}

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem(ACCESS_TOKEN_KEY);
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const status = error.response?.status;
    const config = error.config as InternalAxiosRequestConfig & { _retried?: boolean };

    if (status !== 401 || !config || config._retried) {
      if (status === 401) onUnauthorized();
      return Promise.reject(error);
    }

    const isRefreshRequest = typeof config.url === 'string' && config.url.includes(AUTH_REFRESH_URL);
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
    if (isRefreshRequest || !refreshToken) {
      onUnauthorized();
      return Promise.reject(error);
    }

    config._retried = true;
    try {
      const { data } = await axios.post<{ success?: boolean; data?: { accessToken?: string; refreshToken?: string } }>(
        baseURL + AUTH_REFRESH_URL,
        { refreshToken },
        { headers: { 'Content-Type': 'application/json' } }
      );
      if (data?.success && data?.data?.accessToken) {
        localStorage.setItem(ACCESS_TOKEN_KEY, data.data.accessToken);
        if (data.data.refreshToken) localStorage.setItem(REFRESH_TOKEN_KEY, data.data.refreshToken);
        config.headers.Authorization = `Bearer ${data.data.accessToken}`;
        return apiClient.request(config);
      }
    } catch {
      /* refresh failed */
    }
    onUnauthorized();
    return Promise.reject(error);
  }
);
