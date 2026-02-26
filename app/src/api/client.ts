import axios, { type AxiosError } from 'axios';

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
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401) {
      onUnauthorized();
    }
    return Promise.reject(error);
  }
);
