/** Permission names used for feature visibility and access control. */
export const PERMISSIONS = {
  ViewAdminSettings: 'ViewAdminSettings',
  ProjectView: 'Project.View',
  ProjectCreate: 'Project.Create',
  ProjectEdit: 'Project.Edit',
  ProjectDelete: 'Project.Delete',
  TaskView: 'Task.View',
  TaskCreate: 'Task.Create',
  TaskEdit: 'Task.Edit',
  TaskDelete: 'Task.Delete',
  TaskAssign: 'Task.Assign',
  TaskComment: 'Task.Comment',
  TaskViewActivity: 'Task.ViewActivity',
  // Standalone Tasks module
  TasksView: 'Tasks.View',
  TasksCreate: 'Tasks.Create',
  TasksEdit: 'Tasks.Edit',
  TasksDelete: 'Tasks.Delete',
  TasksAssign: 'Tasks.Assign',
} as const;

export type PermissionName = (typeof PERMISSIONS)[keyof typeof PERMISSIONS];
