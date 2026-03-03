/** Platform Admin (Super Admin) dashboard and tenant management DTOs. */

export interface PlatformDashboardStatsResponse {
  totalTenants: number;
  activeSubscriptions: number;
  monthlyRecurringRevenue: number;
  trialUsers: number;
  churnedUsers: number;
}

export interface PlatformTenantListItemResponse {
  id: string;
  name: string;
  slug: string;
  status: string;
  planName: string | null;
  subscriptionStatus: string | null;
  projectsCount: number;
  usersCount: number;
  storageUsedMB: number;
  timeLogsCount: number;
}

export interface TenantUsageDetailResponse {
  tenantId: string;
  tenantName: string;
  slug: string;
  status: string;
  projectsCount: number;
  usersCount: number;
  storageUsedMB: number;
  timeLogsCount: number;
}

export interface UpgradeTenantRequest {
  planId: string;
}

export interface SetTenantStatusRequest {
  status: string; // Active | Suspended | Cancelled
}
