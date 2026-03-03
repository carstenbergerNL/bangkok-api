import { apiClient } from '../api/client';
import type { ApiResponse } from '../models/ApiResponse';
import { API_PATHS } from '../constants/api';

export interface PlanResponse {
  id: string;
  name: string;
  priceMonthly?: number;
  priceYearly?: number;
  maxProjects?: number;
  maxUsers?: number;
  automationEnabled: boolean;
  stripePriceIdMonthly?: string | null;
  stripePriceIdYearly?: string | null;
  storageLimitMB?: number | null;
}

export interface SubscriptionUsageResponse {
  plan?: PlanResponse;
  status?: string;
  startDate?: string;
  endDate?: string;
  projectsUsed: number;
  projectsLimit?: number;
  membersUsed: number;
  membersLimit?: number;
  storageUsedMB: number;
  storageLimitMB?: number | null;
  timeLogsUsed: number;
  automationEnabled: boolean;
}

export function getSubscriptionUsage(): Promise<ApiResponse<SubscriptionUsageResponse>> {
  return apiClient.get<ApiResponse<SubscriptionUsageResponse>>(API_PATHS.BILLING.USAGE).then((res) => res.data);
}

export function getPlans(): Promise<ApiResponse<PlanResponse[]>> {
  return apiClient.get<ApiResponse<PlanResponse[]>>(API_PATHS.BILLING.PLANS).then((res) => res.data);
}

export interface CreateCheckoutSessionRequest {
  planId: string;
  billingInterval: 'monthly' | 'yearly';
  successUrl: string;
  cancelUrl: string;
}

export interface CreateCheckoutSessionResponse {
  sessionId: string;
  url: string;
}

export function createCheckoutSession(
  request: CreateCheckoutSessionRequest
): Promise<ApiResponse<CreateCheckoutSessionResponse>> {
  return apiClient
    .post<ApiResponse<CreateCheckoutSessionResponse>>(API_PATHS.BILLING.CREATE_CHECKOUT_SESSION, request)
    .then((res) => res.data)
    .catch((err: { response?: { data?: ApiResponse<CreateCheckoutSessionResponse> }; message?: string }) => {
      const data = err.response?.data;
      if (data && typeof data === 'object' && 'success' in data) return data as ApiResponse<CreateCheckoutSessionResponse>;
      return {
        success: false,
        error: { message: (data as { error?: { message?: string } })?.error?.message ?? err.message ?? 'Failed to create checkout session.' },
      } as ApiResponse<CreateCheckoutSessionResponse>;
    });
}
