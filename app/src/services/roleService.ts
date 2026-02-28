import { apiClient } from '../api/client';
import type { ApiResponse } from '../models/ApiResponse';
import type { Role, CreateRoleRequest, UpdateRoleRequest } from '../models/Role';
import { API_PATHS } from '../constants';

export function getRoles(): Promise<ApiResponse<Role[]>> {
  return apiClient.get<ApiResponse<Role[]>>(API_PATHS.ROLES.BASE).then((res) => res.data);
}

export function getRoleById(id: string): Promise<ApiResponse<Role>> {
  return apiClient.get<ApiResponse<Role>>(API_PATHS.ROLES.BY_ID(id)).then((res) => res.data);
}

export function createRole(request: CreateRoleRequest): Promise<ApiResponse<Role>> {
  return apiClient
    .post<ApiResponse<Role>>(API_PATHS.ROLES.BASE, request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function updateRole(id: string, request: UpdateRoleRequest): Promise<ApiResponse<Role>> {
  return apiClient
    .put<ApiResponse<Role>>(API_PATHS.ROLES.BY_ID(id), request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function deleteRole(id: string): Promise<ApiResponse<unknown>> {
  return apiClient
    .delete<ApiResponse<unknown>>(API_PATHS.ROLES.BY_ID(id))
    .then((res) => (res.status === 204 || res.data == null ? { success: true } : res.data))
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function assignRoleToUser(userId: string, roleId: string): Promise<void> {
  return apiClient
    .post(API_PATHS.ROLES.ASSIGN_TO_USER(userId, roleId), null, { transformResponse: [(data) => (data === '' || data == null ? null : data)] })
    .then(() => {});
}

export function removeRoleFromUser(userId: string, roleId: string): Promise<void> {
  return apiClient
    .delete(API_PATHS.ROLES.REMOVE_FROM_USER(userId, roleId), { transformResponse: [] })
    .then(() => {});
}
