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
}

export interface UpdateTaskRequest {
  title?: string | null;
  description?: string | null;
  status?: string | null;
  priority?: string | null;
  assignedToUserId?: string | null;
  dueDate?: string | null;
}

export const PROJECT_STATUSES = ['Draft', 'Active', 'Archived'] as const;
export const TASK_STATUSES = ['ToDo', 'InProgress', 'Done'] as const;
export const TASK_PRIORITIES = ['Low', 'Medium', 'High'] as const;
