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

/** Project automation rule – matches backend ProjectAutomationRuleResponse */
export interface ProjectAutomationRule {
  id: string;
  projectId: string;
  trigger: string;
  action: string;
  targetUserId?: string | null;
  targetValue?: string | null;
}

export interface CreateProjectAutomationRuleRequest {
  trigger: string;
  action: string;
  targetUserId?: string | null;
  targetValue?: string | null;
}

/** Project custom field – matches backend ProjectCustomFieldResponse */
export interface ProjectCustomField {
  id: string;
  projectId: string;
  name: string;
  fieldType: string;
  options?: string | null;
  createdAt: string;
}

export interface CreateProjectCustomFieldRequest {
  name: string;
  fieldType: string;
  options?: string | null;
}

export interface UpdateProjectCustomFieldRequest {
  name: string;
  fieldType: string;
  options?: string | null;
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
  estimatedHours?: number | null;
  isRecurring?: boolean;
  recurrencePattern?: string | null;
  recurrenceInterval?: number | null;
  recurrenceEndDate?: string | null;
  recurrenceSourceTaskId?: string | null;
  labels?: Label[];
  customFieldValues?: TaskCustomFieldValue[];
}

/** Task custom field value – matches backend TaskCustomFieldValueResponse */
export interface TaskCustomFieldValue {
  fieldId: string;
  value?: string | null;
}

/** Item for create/update task custom field values */
export interface TaskCustomFieldValueItem {
  fieldId: string;
  value?: string | null;
}

/** Task attachment – matches backend TaskAttachmentResponse */
export interface TaskAttachment {
  id: string;
  taskId: string;
  fileName: string;
  fileSize: number;
  contentType: string;
  uploadedByUserId: string;
  createdAt: string;
}

/** Task time log – matches backend TaskTimeLogResponse */
export interface TaskTimeLog {
  id: string;
  taskId: string;
  userId: string;
  userDisplayName?: string | null;
  hours: number;
  description?: string | null;
  createdAt: string;
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

/** Project template – matches backend ProjectTemplateResponse */
export interface ProjectTemplate {
  id: string;
  name: string;
  description?: string | null;
  createdAt: string;
  tasks: ProjectTemplateTask[];
}

export interface ProjectTemplateTask {
  id: string;
  templateId: string;
  title: string;
  description?: string | null;
  defaultStatus?: string | null;
  defaultPriority?: string | null;
}

export interface CreateProjectTemplateRequest {
  name: string;
  description?: string | null;
  tasks?: CreateProjectTemplateTaskRequest[];
}

/** Same shape as CreateProjectTemplateRequest; used for PUT. */
export interface UpdateProjectTemplateRequest {
  name: string;
  description?: string | null;
  tasks?: CreateProjectTemplateTaskRequest[];
}

export interface CreateProjectTemplateTaskRequest {
  title: string;
  description?: string | null;
  defaultStatus?: string | null;
  defaultPriority?: string | null;
}

export interface CreateTaskRequest {
  projectId: string;
  title: string;
  description?: string | null;
  status?: string;
  priority?: string;
  assignedToUserId?: string | null;
  dueDate?: string | null;
  estimatedHours?: number | null;
  isRecurring?: boolean;
  recurrencePattern?: string | null;
  recurrenceInterval?: number | null;
  recurrenceEndDate?: string | null;
  labelIds?: string[];
  customFieldValues?: TaskCustomFieldValueItem[];
}

export interface UpdateTaskRequest {
  title?: string | null;
  description?: string | null;
  status?: string | null;
  priority?: string | null;
  assignedToUserId?: string | null;
  dueDate?: string | null;
  estimatedHours?: number | null;
  isRecurring?: boolean;
  recurrencePattern?: string | null;
  recurrenceInterval?: number | null;
  recurrenceEndDate?: string | null;
  labelIds?: string[];
  customFieldValues?: TaskCustomFieldValueItem[];
}

export interface CreateTaskTimeLogRequest {
  hours: number;
  description?: string | null;
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

/** Recurrence pattern for recurring tasks – must match backend */
export const RECURRENCE_PATTERNS = ['Daily', 'Weekly', 'Monthly'] as const;
export type RecurrencePattern = (typeof RECURRENCE_PATTERNS)[number];
