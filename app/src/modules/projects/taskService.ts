import { apiClient } from '../../api/client';
import { API_PATHS } from '../../constants/api';
import type { ApiResponse } from '../../models/ApiResponse';
import type { Task, CreateTaskRequest, UpdateTaskRequest, TaskFilterParams, TaskTimeLog, CreateTaskTimeLogRequest, TaskAttachment } from './types';

export function getTasks(projectId: string, filter?: TaskFilterParams): Promise<ApiResponse<Task[]>> {
  const params: Record<string, string> = { projectId };
  if (filter) {
    if (filter.status) params.status = filter.status;
    if (filter.priority) params.priority = filter.priority;
    if (filter.assignedToUserId) params.assignedToUserId = filter.assignedToUserId;
    if (filter.labelId) params.labelId = filter.labelId;
    if (filter.dueBefore) params.dueBefore = filter.dueBefore;
    if (filter.dueAfter) params.dueAfter = filter.dueAfter;
    if (filter.search?.trim()) params.search = filter.search.trim();
  }
  return apiClient
    .get<ApiResponse<Task[]>>(API_PATHS.TASKS.BASE, { params })
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function getTask(id: string): Promise<ApiResponse<Task>> {
  return apiClient
    .get<ApiResponse<Task>>(API_PATHS.TASKS.BY_ID(id))
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function createTask(request: CreateTaskRequest): Promise<ApiResponse<Task>> {
  return apiClient
    .post<ApiResponse<Task>>(API_PATHS.TASKS.BASE, request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function updateTask(id: string, request: UpdateTaskRequest): Promise<ApiResponse<Task>> {
  return apiClient
    .put<ApiResponse<Task>>(API_PATHS.TASKS.BY_ID(id), request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function deleteTask(id: string): Promise<ApiResponse<unknown>> {
  return apiClient
    .delete(API_PATHS.TASKS.BY_ID(id))
    .then(() => ({ success: true } as ApiResponse<unknown>))
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function getTimeLogs(taskId: string): Promise<ApiResponse<TaskTimeLog[]>> {
  return apiClient
    .get<ApiResponse<TaskTimeLog[]>>(API_PATHS.TASKS.TIMELOGS(taskId))
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export type TimeLogCreateResult =
  | { ok: true; status: number; data: ApiResponse<TaskTimeLog> }
  | { ok: false; status?: number; data: ApiResponse<TaskTimeLog> };

export function createTimeLog(taskId: string, request: CreateTaskTimeLogRequest): Promise<TimeLogCreateResult> {
  return apiClient
    .post<ApiResponse<TaskTimeLog>>(API_PATHS.TASKS.TIMELOGS(taskId), request)
    .then((res) => ({ ok: true, status: res.status, data: res.data }))
    .catch((err: { response?: { status?: number; data?: ApiResponse<TaskTimeLog> }; message?: string }) => ({
      ok: false,
      status: err.response?.status,
      data: err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } },
    }));
}

export function deleteTimeLog(id: string): Promise<ApiResponse<unknown>> {
  return apiClient
    .delete(API_PATHS.TIMELOGS.BY_ID(id))
    .then(() => ({ success: true } as ApiResponse<unknown>))
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function getAttachments(taskId: string): Promise<ApiResponse<TaskAttachment[]>> {
  return apiClient
    .get<ApiResponse<TaskAttachment[]>>(API_PATHS.TASKS.ATTACHMENTS(taskId))
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function uploadAttachment(taskId: string, file: File): Promise<ApiResponse<TaskAttachment>> {
  const form = new FormData();
  form.append('file', file);
  return apiClient
    .post<ApiResponse<TaskAttachment>>(API_PATHS.TASKS.ATTACHMENTS(taskId), form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function deleteAttachment(id: string): Promise<ApiResponse<unknown>> {
  return apiClient
    .delete(API_PATHS.ATTACHMENTS.BY_ID(id))
    .then(() => ({ success: true } as ApiResponse<unknown>))
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

/** Trigger download of attachment (fetches with auth, then saves as file). */
export async function downloadAttachment(attachmentId: string, fileName: string): Promise<void> {
  const token = localStorage.getItem('accessToken');
  const baseURL = import.meta.env.DEV ? '' : (import.meta.env.VITE_API_BASE_URL ?? '');
  const url = `${baseURL}${API_PATHS.ATTACHMENTS.DOWNLOAD(attachmentId)}`;
  const res = await fetch(url, { headers: token ? { Authorization: `Bearer ${token}` } : {} });
  if (!res.ok) throw new Error('Download failed');
  const blob = await res.blob();
  const u = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = u;
  a.download = fileName || 'attachment';
  a.click();
  URL.revokeObjectURL(u);
}
