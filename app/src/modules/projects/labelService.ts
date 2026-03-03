import { apiClient } from '../../api/client';
import { API_PATHS } from '../../constants/api';
import type { ApiResponse } from '../../models/ApiResponse';
import type { Label, CreateLabelRequest } from './types';

export function getLabels(projectId: string): Promise<ApiResponse<Label[]>> {
  return apiClient
    .get<ApiResponse<Label[]>>(API_PATHS.PROJECTS.LABELS(projectId))
    .then((res) => res.data)
    .catch(
      (err: { response?: { data?: ApiResponse<Label[]> } }) =>
        err.response?.data ?? { success: false, error: { message: 'Request failed' } }
    );
}

export function createLabel(projectId: string, request: CreateLabelRequest): Promise<ApiResponse<Label>> {
  return apiClient
    .post<ApiResponse<Label>>(API_PATHS.PROJECTS.LABELS(projectId), request)
    .then((res) => res.data)
    .catch(
      (err: { response?: { data?: ApiResponse<Label> } }) =>
        err.response?.data ?? { success: false, error: { message: 'Request failed' } }
    );
}

export function deleteLabel(projectId: string, labelId: string): Promise<ApiResponse<unknown>> {
  return apiClient
    .delete(API_PATHS.PROJECTS.LABEL_BY_ID(projectId, labelId))
    .then(() => ({ success: true } as ApiResponse<unknown>))
    .catch(
      (err: { response?: { data?: ApiResponse<unknown> } }) =>
        err.response?.data ?? { success: false, error: { message: 'Request failed' } }
    );
}
