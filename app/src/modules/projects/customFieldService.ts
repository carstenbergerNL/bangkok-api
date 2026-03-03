import { apiClient } from '../../api/client';
import { API_PATHS } from '../../constants/api';
import type { ApiResponse } from '../../models/ApiResponse';
import type {
  ProjectCustomField,
  CreateProjectCustomFieldRequest,
  UpdateProjectCustomFieldRequest,
} from './types';

export function getCustomFields(projectId: string): Promise<ApiResponse<ProjectCustomField[]>> {
  return apiClient
    .get<ApiResponse<ProjectCustomField[]>>(API_PATHS.PROJECTS.CUSTOM_FIELDS(projectId))
    .then((res) => res.data)
    .catch(
      (err: { response?: { data?: ApiResponse<ProjectCustomField[]> } }) =>
        err.response?.data ?? { success: false, error: { message: 'Request failed' } }
    );
}

export function createCustomField(
  projectId: string,
  request: CreateProjectCustomFieldRequest
): Promise<ApiResponse<ProjectCustomField>> {
  return apiClient
    .post<ApiResponse<ProjectCustomField>>(API_PATHS.PROJECTS.CUSTOM_FIELDS(projectId), request)
    .then((res) => res.data)
    .catch(
      (err: { response?: { data?: ApiResponse<ProjectCustomField> } }) =>
        err.response?.data ?? { success: false, error: { message: 'Request failed' } }
    );
}

export function updateCustomField(
  projectId: string,
  fieldId: string,
  request: UpdateProjectCustomFieldRequest
): Promise<ApiResponse<ProjectCustomField>> {
  return apiClient
    .put<ApiResponse<ProjectCustomField>>(API_PATHS.PROJECTS.CUSTOM_FIELD_BY_ID(projectId, fieldId), request)
    .then((res) => res.data)
    .catch(
      (err: { response?: { data?: ApiResponse<ProjectCustomField> } }) =>
        err.response?.data ?? { success: false, error: { message: 'Request failed' } }
    );
}

export function deleteCustomField(projectId: string, fieldId: string): Promise<ApiResponse<unknown>> {
  return apiClient
    .delete(API_PATHS.PROJECTS.CUSTOM_FIELD_BY_ID(projectId, fieldId))
    .then(() => ({ success: true } as ApiResponse<unknown>))
    .catch(
      (err: { response?: { data?: ApiResponse<unknown> } }) =>
        err.response?.data ?? { success: false, error: { message: 'Request failed' } }
    );
}
