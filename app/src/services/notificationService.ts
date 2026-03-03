import { apiClient } from '../api/client';
import { API_PATHS } from '../constants/api';
import type { ApiResponse } from '../models/ApiResponse';

export interface Notification {
  id: string;
  userId: string;
  type: string;
  title: string;
  message: string;
  referenceId?: string | null;
  isRead: boolean;
  createdAt: string;
}

export function getNotifications(): Promise<ApiResponse<Notification[]>> {
  return apiClient.get<ApiResponse<Notification[]>>(API_PATHS.NOTIFICATIONS.BASE).then((res) => res.data);
}

export function getUnreadCount(): Promise<ApiResponse<number>> {
  return apiClient.get<ApiResponse<number>>(API_PATHS.NOTIFICATIONS.UNREAD_COUNT).then((res) => res.data);
}

export function markNotificationRead(id: string): Promise<ApiResponse<unknown>> {
  return apiClient
    .put<ApiResponse<unknown>>(API_PATHS.NOTIFICATIONS.MARK_READ(id))
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function markAllNotificationsRead(): Promise<ApiResponse<unknown>> {
  return apiClient
    .put<ApiResponse<unknown>>(API_PATHS.NOTIFICATIONS.MARK_ALL_READ)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}
