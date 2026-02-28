/**
 * Role service – mirrors app (web). Same endpoints and types. Keep in sync.
 */
import { API_PATHS } from '../constants';
import type { ApiResponse, Role, CreateRoleRequest, UpdateRoleRequest } from '../models';
import { apiRequest } from '../api/client';

export function getRoles(): Promise<ApiResponse<Role[]>> {
  return apiRequest<ApiResponse<Role[]>>(API_PATHS.ROLES.BASE);
}

export function getRoleById(id: string): Promise<ApiResponse<Role>> {
  return apiRequest<ApiResponse<Role>>(API_PATHS.ROLES.BY_ID(id));
}

export function createRole(request: CreateRoleRequest): Promise<ApiResponse<Role>> {
  return apiRequest<ApiResponse<Role>>(API_PATHS.ROLES.BASE, { method: 'POST', body: request });
}

export function updateRole(id: string, request: UpdateRoleRequest): Promise<ApiResponse<Role>> {
  return apiRequest<ApiResponse<Role>>(API_PATHS.ROLES.BY_ID(id), { method: 'PUT', body: request });
}

export function deleteRole(id: string): Promise<ApiResponse<unknown>> {
  return apiRequest<ApiResponse<unknown>>(API_PATHS.ROLES.BY_ID(id), { method: 'DELETE' });
}

export function assignRoleToUser(userId: string, roleId: string): Promise<void> {
  return apiRequest<void>(API_PATHS.ROLES.ASSIGN_TO_USER(userId, roleId), { method: 'POST' });
}
