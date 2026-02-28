import { apiClient } from '../api/client';
import type { ApiResponse } from '../models/ApiResponse';
import type { Permission, CreatePermissionRequest, UpdatePermissionRequest } from '../models/Permission';
import { API_PATHS } from '../constants';

export function getPermissions(): Promise<ApiResponse<Permission[]>> {
  return apiClient.get<ApiResponse<Permission[]>>(API_PATHS.PERMISSIONS.BASE).then((res) => res.data);
}

export function getPermissionById(id: string): Promise<ApiResponse<Permission>> {
  return apiClient.get<ApiResponse<Permission>>(API_PATHS.PERMISSIONS.BY_ID(id)).then((res) => res.data);
}

export function createPermission(request: CreatePermissionRequest): Promise<ApiResponse<Permission>> {
  return apiClient
    .post<ApiResponse<Permission>>(API_PATHS.PERMISSIONS.BASE, request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function updatePermission(id: string, request: UpdatePermissionRequest): Promise<ApiResponse<Permission>> {
  return apiClient
    .put<ApiResponse<Permission>>(API_PATHS.PERMISSIONS.BY_ID(id), request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function deletePermission(id: string): Promise<ApiResponse<unknown>> {
  return apiClient
    .delete(API_PATHS.PERMISSIONS.BY_ID(id))
    .then(() => ({ success: true } as ApiResponse<unknown>))
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function getPermissionsForRole(roleId: string): Promise<ApiResponse<Permission[]>> {
  return apiClient.get<ApiResponse<Permission[]>>(API_PATHS.ROLES.PERMISSIONS(roleId)).then((res) => res.data);
}

export function assignPermissionToRole(roleId: string, permissionId: string): Promise<void> {
  return apiClient.post(API_PATHS.ROLES.ASSIGN_PERMISSION(roleId, permissionId)).then(() => {});
}

export function removePermissionFromRole(roleId: string, permissionId: string): Promise<void> {
  return apiClient.delete(API_PATHS.ROLES.ASSIGN_PERMISSION(roleId, permissionId)).then(() => {});
}
