/**
 * Standalone Tasks module API. Keep in sync with app (web) tasksService.
 */
import type { ApiResponse } from '../../models/ApiResponse';
import type {
  TasksStandaloneTask,
  TasksStandaloneFilter,
  CreateTasksStandaloneRequest,
  UpdateTasksStandaloneRequest,
} from './types';

const API_BASE = ''; // use env or relative

function buildParams(filter: TasksStandaloneFilter | undefined): Record<string, string> {
  const params: Record<string, string> = {};
  if (filter?.status?.trim()) params.status = filter.status.trim();
  if (filter?.assignedToUserId) params.assignedToUserId = filter.assignedToUserId;
  if (filter?.priority?.trim()) params.priority = filter.priority.trim();
  if (filter?.dueBefore) params.dueBefore = filter.dueBefore;
  if (filter?.search?.trim()) params.search = filter.search.trim();
  return params;
}

export async function getTasksModuleList(
  filter?: TasksStandaloneFilter
): Promise<ApiResponse<TasksStandaloneTask[]>> {
  const params = buildParams(filter);
  const qs = new URLSearchParams(params).toString();
  const url = `${API_BASE}/api/tasks-module${qs ? `?${qs}` : ''}`;
  const res = await fetch(url, { headers: { Accept: 'application/json' } });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) return { success: false, error: data?.error ?? { message: 'Request failed' } };
  return data;
}

export async function getTasksModuleMy(
  filter?: Pick<TasksStandaloneFilter, 'status' | 'priority' | 'dueBefore' | 'search'>
): Promise<ApiResponse<TasksStandaloneTask[]>> {
  const params: Record<string, string> = {};
  if (filter?.status?.trim()) params.status = filter.status.trim();
  if (filter?.priority?.trim()) params.priority = filter.priority.trim();
  if (filter?.dueBefore) params.dueBefore = filter.dueBefore;
  if (filter?.search?.trim()) params.search = filter.search.trim();
  const qs = new URLSearchParams(params).toString();
  const url = `${API_BASE}/api/tasks-module/my${qs ? `?${qs}` : ''}`;
  const res = await fetch(url, { headers: { Accept: 'application/json' } });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) return { success: false, error: data?.error ?? { message: 'Request failed' } };
  return data;
}

export async function getTasksModuleById(id: string): Promise<ApiResponse<TasksStandaloneTask>> {
  const res = await fetch(`${API_BASE}/api/tasks-module/${id}`, { headers: { Accept: 'application/json' } });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) return { success: false, error: data?.error ?? { message: 'Request failed' } };
  return data;
}

export async function createTasksModule(
  request: CreateTasksStandaloneRequest
): Promise<ApiResponse<TasksStandaloneTask>> {
  const res = await fetch(`${API_BASE}/api/tasks-module`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify(request),
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) return { success: false, error: data?.error ?? { message: 'Request failed' } };
  return data;
}

export async function updateTasksModule(
  id: string,
  request: UpdateTasksStandaloneRequest
): Promise<ApiResponse<TasksStandaloneTask>> {
  const res = await fetch(`${API_BASE}/api/tasks-module/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify(request),
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) return { success: false, error: data?.error ?? { message: 'Request failed' } };
  return data;
}

export async function deleteTasksModule(id: string): Promise<ApiResponse<unknown>> {
  const res = await fetch(`${API_BASE}/api/tasks-module/${id}`, { method: 'DELETE' });
  if (res.status === 204) return { success: true };
  const data = await res.json().catch(() => ({}));
  return { success: false, error: data?.error ?? { message: 'Request failed' } };
}

export async function setTasksModuleStatus(
  id: string,
  status: string
): Promise<ApiResponse<TasksStandaloneTask>> {
  const res = await fetch(`${API_BASE}/api/tasks-module/${id}/status?status=${encodeURIComponent(status)}`, {
    method: 'PATCH',
    headers: { Accept: 'application/json' },
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) return { success: false, error: data?.error ?? { message: 'Request failed' } };
  return data;
}
