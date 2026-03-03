import { apiClient } from '../../api/client';
import { API_PATHS } from '../../constants/api';
import type { ApiResponse } from '../../models/ApiResponse';
import type { ProjectDashboard } from './types';

export function getProjectDashboard(projectId: string): Promise<ApiResponse<ProjectDashboard>> {
  return apiClient
    .get<ApiResponse<ProjectDashboard>>(API_PATHS.PROJECTS.DASHBOARD(projectId))
    .then((res) => res.data)
    .catch(
      (err: { response?: { data?: ApiResponse<ProjectDashboard> } }) =>
        err.response?.data ?? { success: false, error: { message: 'Request failed' } }
    );
}
