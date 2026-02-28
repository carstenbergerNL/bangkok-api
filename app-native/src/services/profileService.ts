/**
 * Profile service – mirrors app (web). Same endpoints and types. Keep in sync.
 */
import { API_PATHS } from '../constants';
import type { ApiResponse, Profile, CreateProfileRequest, UpdateProfileRequest } from '../models';
import { apiRequest } from '../api/client';

export function getProfileByUserId(userId: string): Promise<ApiResponse<Profile>> {
  return apiRequest<ApiResponse<Profile>>(API_PATHS.PROFILE.BY_USER_ID(userId));
}

export function createProfile(request: CreateProfileRequest): Promise<ApiResponse<Profile>> {
  return apiRequest<ApiResponse<Profile>>(API_PATHS.PROFILE.BASE, { method: 'POST', body: request });
}

export function updateProfile(userId: string, request: UpdateProfileRequest): Promise<ApiResponse<Profile>> {
  return apiRequest<ApiResponse<Profile>>(API_PATHS.PROFILE.BY_USER_ID(userId), { method: 'PUT', body: request });
}
