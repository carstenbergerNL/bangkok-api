import { apiClient } from '../../api/client';
import { API_PATHS } from '../../constants/api';
import type { ApiResponse } from '../../models/ApiResponse';
import type {
  TasksStandaloneTask,
  TasksStandaloneFilter,
  CreateTasksStandaloneRequest,
  UpdateTasksStandaloneRequest,
} from './types';

function buildParams(filter: TasksStandaloneFilter | undefined): Record<string, string> {
  const params: Record<string, string> = {};
  if (filter?.status?.trim()) params.status = filter.status.trim();
  if (filter?.assignedToUserId) params.assignedToUserId = filter.assignedToUserId;
  if (filter?.priority?.trim()) params.priority = filter.priority.trim();
  if (filter?.dueBefore) params.dueBefore = filter.dueBefore;
  if (filter?.search?.trim()) params.search = filter.search.trim();
  return params;
}

export function getTasksModuleList(filter?: TasksStandaloneFilter): Promise<ApiResponse<TasksStandaloneTask[]>> {
  const params = Object.keys(buildParams(filter)).length ? { params: buildParams(filter) } : {};
  return apiClient
    .get<ApiResponse<TasksStandaloneTask[]>>(API_PATHS.TASKS_MODULE.BASE, params)
    .then((res) => res.data)
    .catch(
      (err: { response?: { data?: ApiResponse<TasksStandaloneTask[]> } }) =>
        err.response?.data ?? { success: false, error: { message: 'Request failed' } }
    );
}

export function getTasksModuleMy(
  filter?: Pick<TasksStandaloneFilter, 'status' | 'priority' | 'dueBefore' | 'search'>
): Promise<ApiResponse<TasksStandaloneTask[]>> {
  const params: Record<string, string> = {};
  if (filter?.status?.trim()) params.status = filter.status.trim();
  if (filter?.priority?.trim()) params.priority = filter.priority.trim();
  if (filter?.dueBefore) params.dueBefore = filter.dueBefore;
  if (filter?.search?.trim()) params.search = filter.search.trim();
  const query = Object.keys(params).length ? { params } : {};
  return apiClient
    .get<ApiResponse<TasksStandaloneTask[]>>(API_PATHS.TASKS_MODULE.MY, query)
    .then((res) => res.data)
    .catch(
      (err: { response?: { data?: ApiResponse<TasksStandaloneTask[]> } }) =>
        err.response?.data ?? { success: false, error: { message: 'Request failed' } }
    );
}

export function getTasksModuleById(id: string): Promise<ApiResponse<TasksStandaloneTask>> {
  return apiClient
    .get<ApiResponse<TasksStandaloneTask>>(API_PATHS.TASKS_MODULE.BY_ID(id))
    .then((res) => res.data)
    .catch(
      (err: { response?: { data?: ApiResponse<TasksStandaloneTask> } }) =>
        err.response?.data ?? { success: false, error: { message: 'Request failed' } }
    );
}

export function createTasksModule(request: CreateTasksStandaloneRequest): Promise<ApiResponse<TasksStandaloneTask>> {
  return apiClient
    .post<ApiResponse<TasksStandaloneTask>>(API_PATHS.TASKS_MODULE.BASE, request)
    .then((res) => res.data)
    .catch(
      (err: { response?: { data?: ApiResponse<TasksStandaloneTask> } }) =>
        err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } }
    );
}

export function updateTasksModule(id: string, request: UpdateTasksStandaloneRequest): Promise<ApiResponse<TasksStandaloneTask>> {
  return apiClient
    .put<ApiResponse<TasksStandaloneTask>>(API_PATHS.TASKS_MODULE.BY_ID(id), request)
    .then((res) => res.data)
    .catch(
      (err: { response?: { data?: ApiResponse<TasksStandaloneTask> } }) =>
        err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } }
    );
}

export function deleteTasksModule(id: string): Promise<ApiResponse<unknown>> {
  return apiClient
    .delete(API_PATHS.TASKS_MODULE.BY_ID(id))
    .then(() => ({ success: true } as ApiResponse<unknown>))
    .catch(
      (err: { response?: { data?: ApiResponse<unknown> } }) =>
        err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } }
    );
}

export function setTasksModuleStatus(id: string, status: string): Promise<ApiResponse<TasksStandaloneTask>> {
  return apiClient
    .patch<ApiResponse<TasksStandaloneTask>>(API_PATHS.TASKS_MODULE.STATUS(id), null, { params: { status } })
    .then((res) => res.data)
    .catch(
      (err: { response?: { data?: ApiResponse<TasksStandaloneTask> } }) =>
        err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } }
    );
}
