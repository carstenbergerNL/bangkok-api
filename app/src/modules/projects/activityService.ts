import { apiClient } from '../../api/client';
import { API_PATHS } from '../../constants/api';
import type { ApiResponse } from '../../models/ApiResponse';
import type { TaskActivity } from './types';

export function getActivitiesByTaskId(taskId: string): Promise<ApiResponse<TaskActivity[]>> {
  return apiClient
    .get<ApiResponse<TaskActivity[]>>(API_PATHS.TASKS.ACTIVITIES(taskId))
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}
