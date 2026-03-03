import { apiClient } from '../api/client';
import type { ApiResponse } from '../models/ApiResponse';
import type {
  PlatformDashboardStatsResponse,
  PlatformTenantListItemResponse,
  TenantUsageDetailResponse,
  UpgradeTenantRequest,
  SetTenantStatusRequest,
} from '../models/PlatformAdmin';
import { API_PATHS } from '../constants/api';

export function getDashboardStats(): Promise<ApiResponse<PlatformDashboardStatsResponse>> {
  return apiClient.get<ApiResponse<PlatformDashboardStatsResponse>>(API_PATHS.PLATFORM_ADMIN.DASHBOARD_STATS).then((res) => res.data);
}

export function getTenants(): Promise<ApiResponse<PlatformTenantListItemResponse[]>> {
  return apiClient.get<ApiResponse<PlatformTenantListItemResponse[]>>(API_PATHS.PLATFORM_ADMIN.TENANTS).then((res) => res.data);
}

export function getTenantUsage(tenantId: string): Promise<ApiResponse<TenantUsageDetailResponse>> {
  return apiClient.get<ApiResponse<TenantUsageDetailResponse>>(API_PATHS.PLATFORM_ADMIN.TENANT_USAGE(tenantId)).then((res) => res.data);
}

export function suspendTenant(tenantId: string): Promise<ApiResponse<unknown>> {
  return apiClient.put<ApiResponse<unknown>>(API_PATHS.PLATFORM_ADMIN.TENANT_SUSPEND(tenantId)).then((res) => res.data);
}

export function setTenantStatus(tenantId: string, request: SetTenantStatusRequest): Promise<ApiResponse<unknown>> {
  return apiClient.put<ApiResponse<unknown>>(API_PATHS.PLATFORM_ADMIN.TENANT_STATUS(tenantId), request).then((res) => res.data);
}

export function upgradeTenant(tenantId: string, request: UpgradeTenantRequest): Promise<ApiResponse<unknown>> {
  return apiClient.put<ApiResponse<unknown>>(API_PATHS.PLATFORM_ADMIN.TENANT_UPGRADE(tenantId), request).then((res) => res.data);
}
