import { apiClient } from '../api/client';
import type { ApiResponse } from '../models/ApiResponse';
import { API_PATHS } from '../constants/api';

export interface ActiveModulesResponse {
  activeModuleKeys: string[];
}

export interface TenantModuleListItem {
  tenantModuleId: string;
  moduleId: string;
  name: string;
  key: string;
  description?: string;
  isActive: boolean;
}

export function getActiveModules(): Promise<ApiResponse<ActiveModulesResponse>> {
  return apiClient.get<ApiResponse<ActiveModulesResponse>>(API_PATHS.TENANT_MODULES.BASE).then((res) => res.data);
}

export function getTenantModulesManagement(): Promise<ApiResponse<TenantModuleListItem[]>> {
  return apiClient.get<ApiResponse<TenantModuleListItem[]>>(API_PATHS.TENANT_MODULES.MANAGEMENT).then((res) => res.data);
}

export function setModuleActive(moduleKey: string, isActive: boolean): Promise<void> {
  return apiClient.put(API_PATHS.TENANT_MODULES.SET_ACTIVE(moduleKey), { isActive }).then(() => undefined);
}
