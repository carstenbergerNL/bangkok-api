/** Standalone task (Tasks module). Not tied to Projects. */
export interface TasksStandaloneTask {
  id: string;
  tenantId: string;
  title: string;
  description: string | null;
  status: string;
  priority: string;
  assignedToUserId: string | null;
  assignedToDisplayName: string | null;
  createdByUserId: string;
  dueDate: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface TasksStandaloneFilter {
  status?: string;
  assignedToUserId?: string;
  priority?: string;
  dueBefore?: string;
  search?: string;
}

export interface CreateTasksStandaloneRequest {
  title: string;
  description?: string | null;
  status?: string;
  priority?: string;
  assignedToUserId?: string | null;
  dueDate?: string | null;
}

export interface UpdateTasksStandaloneRequest {
  title?: string;
  description?: string | null;
  status?: string;
  priority?: string;
  assignedToUserId?: string | null;
  dueDate?: string | null;
}
