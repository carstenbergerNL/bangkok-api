import { apiClient } from '../../api/client';
import { API_PATHS } from '../../constants/api';
import type { ApiResponse } from '../../models/ApiResponse';
import type { TaskComment, CreateTaskCommentRequest, UpdateTaskCommentRequest } from './types';

export function getCommentsByTaskId(taskId: string): Promise<ApiResponse<TaskComment[]>> {
  return apiClient
    .get<ApiResponse<TaskComment[]>>(API_PATHS.TASKS.COMMENTS(taskId))
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function createComment(taskId: string, request: CreateTaskCommentRequest): Promise<ApiResponse<TaskComment>> {
  return apiClient
    .post<ApiResponse<TaskComment>>(API_PATHS.TASKS.COMMENTS(taskId), request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function updateComment(id: string, request: UpdateTaskCommentRequest): Promise<ApiResponse<unknown>> {
  return apiClient
    .put<ApiResponse<unknown>>(API_PATHS.COMMENTS.BY_ID(id), request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function deleteComment(id: string): Promise<ApiResponse<unknown>> {
  return apiClient
    .delete(API_PATHS.COMMENTS.BY_ID(id))
    .then(() => ({ success: true } as ApiResponse<unknown>))
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}
