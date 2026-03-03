/** Project – matches backend ProjectResponse */
export interface Project {
  id: string;
  name: string;
  description?: string | null;
  status: string;
  createdByUserId: string;
  createdAt: string;
  updatedAt?: string | null;
}

/** Label – matches backend LabelResponse */
export interface Label {
  id: string;
  name: string;
  color: string;
  projectId: string;
  createdAt: string;
}

/** Task – matches backend TaskResponse */
export interface Task {
  id: string;
  projectId: string;
  title: string;
  description?: string | null;
  status: string;
  priority: string;
  assignedToUserId?: string | null;
  dueDate?: string | null;
  createdByUserId: string;
  createdAt: string;
  updatedAt?: string | null;
  labels?: Label[];
}

export interface CreateProjectRequest {
  name: string;
  description?: string | null;
  status?: string;
}

export interface UpdateProjectRequest {
  name?: string | null;
  description?: string | null;
  status?: string | null;
}

export interface CreateTaskRequest {
  projectId: string;
  title: string;
  description?: string | null;
  status?: string;
  priority?: string;
  assignedToUserId?: string | null;
  dueDate?: string | null;
  labelIds?: string[];
}

export interface UpdateTaskRequest {
  title?: string | null;
  description?: string | null;
  status?: string | null;
  priority?: string | null;
  assignedToUserId?: string | null;
  dueDate?: string | null;
  labelIds?: string[];
}

/** Task comment – matches backend TaskCommentResponse */
export interface TaskComment {
  id: string;
  taskId: string;
  userId: string;
  userDisplayName: string;
  content: string;
  createdAt: string;
  updatedAt?: string | null;
}

/** Task activity – matches backend TaskActivityResponse */
export interface TaskActivity {
  id: string;
  taskId: string;
  activityType: string;
  description: string;
  oldValue?: string | null;
  newValue?: string | null;
  userId: string;
  userDisplayName?: string | null;
  createdAt: string;
}

export interface CreateTaskCommentRequest {
  content: string;
}

export interface UpdateTaskCommentRequest {
  content: string;
}

/** Project member – matches backend ProjectMemberResponse */
export interface ProjectMember {
  id: string;
  projectId: string;
  userId: string;
  userDisplayName: string;
  userEmail: string;
  role: ProjectMemberRole;
  createdAt: string;
}

export type ProjectMemberRole = 'Owner' | 'Member' | 'Viewer';

export const PROJECT_MEMBER_ROLES: ProjectMemberRole[] = ['Owner', 'Member', 'Viewer'];

export interface CreateProjectMemberRequest {
  userId: string;
  role: ProjectMemberRole;
}

export interface UpdateProjectMemberRequest {
  role: ProjectMemberRole;
}

export interface CreateLabelRequest {
  name: string;
  color: string;
}

/** Task list query params – sync with URL and API */
export interface TaskFilterParams {
  status?: string;
  priority?: string;
  assignedToUserId?: string;
  labelId?: string;
  dueBefore?: string; // ISO date YYYY-MM-DD
  dueAfter?: string;
  search?: string;
}

/** Project dashboard – matches backend ProjectDashboardResponse */
export interface ProjectDashboard {
  totalTasks: number;
  completedTasks: number;
  overdueTasks: number;
  tasksPerStatus: { status: string; count: number }[];
  tasksPerMember: { userId: string; userDisplayName: string | null; count: number }[];
}

/** Lifecycle statuses for project filtering and display */
export const PROJECT_STATUSES = ['Active', 'OnHold', 'Completed', 'Archived'] as const;
export type ProjectStatus = (typeof PROJECT_STATUSES)[number];
export const TASK_STATUSES = ['ToDo', 'InProgress', 'Done'] as const;
export const TASK_PRIORITIES = ['Low', 'Medium', 'High'] as const;
