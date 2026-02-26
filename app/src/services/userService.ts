import { apiClient } from '../api/client';
import type { ApiResponse } from '../models/ApiResponse';
import type { PagedResult } from '../models/PagedResult';
import type { User } from '../models/User';
import type { RegisterRequest } from '../models/RegisterRequest';

export interface UpdateUserRequest {
  email?: string;
  displayName?: string;
  role?: string;
  isActive?: boolean;
}

export function getUsers(pageNumber = 1, pageSize = 10, includeDeleted = false): Promise<ApiResponse<PagedResult<User>>> {
  return apiClient
    .get<ApiResponse<PagedResult<User>>>('/api/Users', { params: { pageNumber, pageSize, includeDeleted } })
    .then((res) => res.data);
}

export function getUserById(id: string): Promise<ApiResponse<User>> {
  return apiClient.get<ApiResponse<User>>(`/api/Users/${id}`).then((res) => res.data);
}

/** Create user via Auth/Register. Admin must be logged in. */
export function createUser(request: RegisterRequest): Promise<ApiResponse<unknown>> {
  return apiClient
    .post<ApiResponse<unknown>>('/api/Auth/register', request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function updateUser(id: string, request: UpdateUserRequest): Promise<ApiResponse<User>> {
  return apiClient.put<ApiResponse<User>>(`/api/Users/${id}`, request).then((res) => res.data);
}

/** Soft-delete user. Returns 204 on success; API response shape on error. */
export function deleteUser(id: string): Promise<ApiResponse<unknown>> {
  return apiClient
    .delete(`/api/Users/${id}`)
    .then(() => ({ success: true } as ApiResponse<unknown>))
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

/** Restore a soft-deleted user. Returns 204 on success; API response shape on error. */
export function restoreUser(id: string): Promise<ApiResponse<unknown>> {
  return apiClient
    .patch(`/api/Users/${id}/restore`)
    .then(() => ({ success: true } as ApiResponse<unknown>))
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}
