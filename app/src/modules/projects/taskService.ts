import { apiClient } from '../../api/client';
import { API_PATHS } from '../../constants/api';
import type { ApiResponse } from '../../models/ApiResponse';
import type { Task, CreateTaskRequest, UpdateTaskRequest, TaskFilterParams } from './types';

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
