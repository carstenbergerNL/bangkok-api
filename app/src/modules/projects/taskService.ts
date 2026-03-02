import { apiClient } from '../../api/client';
import { API_PATHS } from '../../constants/api';
import type { ApiResponse } from '../../models/ApiResponse';
import type { Task, CreateTaskRequest, UpdateTaskRequest } from './types';

export function getTasks(projectId: string): Promise<ApiResponse<Task[]>> {
  return apiClient
    .get<ApiResponse<Task[]>>(API_PATHS.TASKS.BASE, { params: { projectId } })
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
