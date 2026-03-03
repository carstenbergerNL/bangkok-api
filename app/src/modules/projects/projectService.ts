import { apiClient } from '../../api/client';
import { API_PATHS } from '../../constants/api';
import type { ApiResponse } from '../../models/ApiResponse';
import type { Project, CreateProjectRequest, UpdateProjectRequest } from './types';

export function getProjects(status?: string): Promise<ApiResponse<Project[]>> {
  const params = status?.trim() ? { params: { status: status.trim() } } : {};
  return apiClient.get<ApiResponse<Project[]>>(API_PATHS.PROJECTS.BASE, params).then((res) => res.data);
}

export function getProject(id: string): Promise<ApiResponse<Project>> {
  return apiClient
    .get<ApiResponse<Project>>(API_PATHS.PROJECTS.BY_ID(id))
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function createProject(request: CreateProjectRequest): Promise<ApiResponse<Project>> {
  return apiClient
    .post<ApiResponse<Project>>(API_PATHS.PROJECTS.BASE, request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function updateProject(id: string, request: UpdateProjectRequest): Promise<ApiResponse<Project>> {
  return apiClient
    .put<ApiResponse<Project>>(API_PATHS.PROJECTS.BY_ID(id), request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function deleteProject(id: string): Promise<ApiResponse<unknown>> {
  return apiClient
    .delete(API_PATHS.PROJECTS.BY_ID(id))
    .then(() => ({ success: true } as ApiResponse<unknown>))
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}
