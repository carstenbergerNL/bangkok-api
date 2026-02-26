import { apiClient } from '../api/client';
import type { ApiResponse } from '../models/ApiResponse';
import type { Profile, CreateProfileRequest, UpdateProfileRequest } from '../models/Profile';

export function getProfileByUserId(userId: string): Promise<ApiResponse<Profile>> {
  return apiClient.get<ApiResponse<Profile>>(`/api/Profile/${userId}`).then((res) => res.data);
}

export function createProfile(request: CreateProfileRequest): Promise<ApiResponse<Profile>> {
  return apiClient
    .post<ApiResponse<Profile>>('/api/Profile', request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function updateProfile(userId: string, request: UpdateProfileRequest): Promise<ApiResponse<Profile>> {
  return apiClient
    .put<ApiResponse<Profile>>(`/api/Profile/${userId}`, request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function deleteProfile(userId: string): Promise<void> {
  return apiClient.delete(`/api/Profile/${userId}`).then(() => {});
}
