/**
 * API client for app-native. Uses fetch; token from SecureStore (to be wired).
 * Base URL from EXPO_PUBLIC_API_BASE_URL. Uses same API_PATHS as web app.
 */
const getBaseURL = (): string => {
  if (typeof process !== 'undefined' && process.env?.EXPO_PUBLIC_API_BASE_URL) {
    return process.env.EXPO_PUBLIC_API_BASE_URL;
  }
  return '';
};

let accessToken: string | null = null;

export function setAccessToken(token: string | null): void {
  accessToken = token;
}

export async function apiRequest<T>(
  path: string,
  options: Omit<RequestInit, 'body'> & { method?: string; body?: unknown } = {}
): Promise<T> {
  const { method = 'GET', body, ...rest } = options;
  const url = path.startsWith('http') ? path : `${getBaseURL()}${path}`;
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...(rest.headers as HeadersInit),
  };
  if (accessToken) {
    (headers as Record<string, string>)['Authorization'] = `Bearer ${accessToken}`;
  }
  const res = await fetch(url, {
    ...rest,
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) {
    throw new Error(data?.error?.message ?? data?.message ?? `Request failed: ${res.status}`);
  }
  return data as T;
}
