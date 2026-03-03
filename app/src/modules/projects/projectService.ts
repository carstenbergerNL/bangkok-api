import { apiClient } from '../../api/client';
import { API_PATHS } from '../../constants/api';
import type { ApiResponse } from '../../models/ApiResponse';
import type { Project, CreateProjectRequest, UpdateProjectRequest, ProjectTemplate, CreateProjectTemplateRequest, UpdateProjectTemplateRequest } from './types';

export function getProjects(status?: string): Promise<ApiResponse<Project[]>> {
  const params = status?.trim() ? { params: { status: status.trim() } } : {};
  return apiClient.get<ApiResponse<Project[]>>(API_PATHS.PROJECTS.BASE, params).then((res) => res.data);
}

export function getProject(id: string): Promise<ApiResponse<Project>> {
  return apiClient
    .get<ApiResponse<Project>>(API_PATHS.PROJECTS.BY_ID(id))
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

/** Triggers download of project tasks CSV. Returns a promise that resolves when done or rejects on error. */
export function exportProjectToCsv(projectId: string): Promise<void> {
  return apiClient
    .get(API_PATHS.PROJECTS.EXPORT(projectId), { responseType: 'blob' })
    .then((res) => {
      if (res.status !== 200 || !(res.data instanceof Blob)) {
        throw new Error(res.status === 404 ? 'Project not found.' : res.status === 403 ? 'Access denied.' : 'Export failed.');
      }
      const blob = res.data as Blob;
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `project-${projectId}.csv`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    });
}

export function createProject(request: CreateProjectRequest): Promise<ApiResponse<Project>> {
  return apiClient
    .post<ApiResponse<Project>>(API_PATHS.PROJECTS.BASE, request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function createProjectFromTemplate(templateId: string, request: CreateProjectRequest): Promise<ApiResponse<Project>> {
  return apiClient
    .post<ApiResponse<Project>>(API_PATHS.PROJECTS.FROM_TEMPLATE(templateId), request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function getProjectTemplates(): Promise<ApiResponse<ProjectTemplate[]>> {
  return apiClient
    .get<ApiResponse<ProjectTemplate[]>>(API_PATHS.PROJECT_TEMPLATES.BASE)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function getProjectTemplate(id: string): Promise<ApiResponse<ProjectTemplate>> {
  return apiClient
    .get<ApiResponse<ProjectTemplate>>(API_PATHS.PROJECT_TEMPLATES.BY_ID(id))
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function createProjectTemplate(request: CreateProjectTemplateRequest): Promise<ApiResponse<ProjectTemplate>> {
  return apiClient
    .post<ApiResponse<ProjectTemplate>>(API_PATHS.PROJECT_TEMPLATES.BASE, request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function updateProjectTemplate(id: string, request: UpdateProjectTemplateRequest): Promise<ApiResponse<ProjectTemplate>> {
  return apiClient
    .put<ApiResponse<ProjectTemplate>>(API_PATHS.PROJECT_TEMPLATES.BY_ID(id), request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function deleteProjectTemplate(id: string): Promise<ApiResponse<unknown>> {
  return apiClient
    .delete(API_PATHS.PROJECT_TEMPLATES.BY_ID(id))
    .then(() => ({ success: true } as ApiResponse<unknown>))
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function updateProject(id: string, request: UpdateProjectRequest): Promise<ApiResponse<Project>> {
  return apiClient
    .put<ApiResponse<Project>>(API_PATHS.PROJECTS.BY_ID(id), request)
    .then((res) => res.data)
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}

export function deleteProject(id: string): Promise<ApiResponse<unknown>> {
  return apiClient
    .delete(API_PATHS.PROJECTS.BY_ID(id))
    .then(() => ({ success: true } as ApiResponse<unknown>))
    .catch((err) => err.response?.data ?? { success: false, error: { message: err.message ?? 'Request failed' } });
}
