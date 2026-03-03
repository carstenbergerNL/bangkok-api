import { apiClient } from '../../api/client';
import { API_PATHS } from '../../constants/api';
import type { ApiResponse } from '../../models/ApiResponse';
import type {
  ProjectMember,
  CreateProjectMemberRequest,
  UpdateProjectMemberRequest,
  ProjectMemberRole,
} from './types';

export interface MyRoleResponse {
  role: ProjectMemberRole;
}

export function getMyProjectRole(projectId: string): Promise<ApiResponse<MyRoleResponse>> {
  return apiClient
    .get<ApiResponse<MyRoleResponse>>(API_PATHS.PROJECTS.MEMBERS_ME(projectId))
    .then((res) => res.data)
    .catch(
      (err: { response?: { data?: ApiResponse<MyRoleResponse> } }) =>
        err.response?.data ?? { success: false, error: { message: 'Request failed' } }
    );
}

export function getProjectMembers(projectId: string): Promise<ApiResponse<ProjectMember[]>> {
  return apiClient
    .get<ApiResponse<ProjectMember[]>>(API_PATHS.PROJECTS.MEMBERS(projectId))
    .then((res) => res.data)
    .catch(
      (err: { response?: { data?: ApiResponse<ProjectMember[]> } }) =>
        err.response?.data ?? { success: false, error: { message: 'Request failed' } }
    );
}

export function addProjectMember(
  projectId: string,
  request: CreateProjectMemberRequest
): Promise<ApiResponse<ProjectMember>> {
  return apiClient
    .post<ApiResponse<ProjectMember>>(API_PATHS.PROJECTS.MEMBERS(projectId), request)
    .then((res) => res.data)
    .catch(
      (err: { response?: { data?: ApiResponse<ProjectMember> } }) =>
        err.response?.data ?? { success: false, error: { message: 'Request failed' } }
    );
}

export function updateProjectMemberRole(
  projectId: string,
  memberId: string,
  request: UpdateProjectMemberRequest
): Promise<ApiResponse<ProjectMember>> {
  return apiClient
    .put<ApiResponse<ProjectMember>>(API_PATHS.PROJECTS.MEMBER_BY_ID(projectId, memberId), request)
    .then((res) => res.data)
    .catch(
      (err: { response?: { data?: ApiResponse<ProjectMember> } }) =>
        err.response?.data ?? { success: false, error: { message: 'Request failed' } }
    );
}

export function removeProjectMember(
  projectId: string,
  memberId: string
): Promise<ApiResponse<unknown>> {
  return apiClient
    .delete(API_PATHS.PROJECTS.MEMBER_BY_ID(projectId, memberId))
    .then(() => ({ success: true } as ApiResponse<unknown>))
    .catch(
      (err: { response?: { data?: ApiResponse<unknown> } }) =>
        err.response?.data ?? { success: false, error: { message: 'Request failed' } }
    );
}
