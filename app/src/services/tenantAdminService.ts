import { apiClient } from '../api/client';
import type { ApiResponse } from '../models/ApiResponse';
import { API_PATHS } from '../constants/api';

export interface TenantAdminUser {
  userId: string;
  email: string;
  displayName?: string;
  tenantRole: string;
  activeModules: string[];
}

export interface InviteTenantUserRequest {
  email: string;
  tenantRole: string;
  moduleKeys?: string[];
}

export function getTenantAdminUsers(): Promise<ApiResponse<TenantAdminUser[]>> {
  return apiClient
    .get<ApiResponse<TenantAdminUser[]>>(API_PATHS.TENANT_ADMIN.USERS)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Failed to load users.' } });
}

export function inviteTenantUser(request: InviteTenantUserRequest): Promise<ApiResponse<TenantAdminUser>> {
  return apiClient
    .post<ApiResponse<TenantAdminUser>>(API_PATHS.TENANT_ADMIN.USERS, request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Failed to add user.' } });
}

export function removeTenantUser(userId: string): Promise<ApiResponse<unknown>> {
  return apiClient
    .delete(API_PATHS.TENANT_ADMIN.USER_BY_ID(userId))
    .then(() => ({ success: true } as ApiResponse<unknown>))
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Failed to remove user.' } });
}

export function updateTenantUserRole(userId: string, role: string): Promise<ApiResponse<unknown>> {
  return apiClient
    .put(API_PATHS.TENANT_ADMIN.USER_ROLE(userId), { role })
    .then(() => ({ success: true } as ApiResponse<unknown>))
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Failed to update role.' } });
}

export function updateTenantUserModules(userId: string, moduleKeys: string[]): Promise<ApiResponse<unknown>> {
  return apiClient
    .put(API_PATHS.TENANT_ADMIN.USER_MODULES(userId), { moduleKeys })
    .then(() => ({ success: true } as ApiResponse<unknown>))
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Failed to update modules.' } });
}
