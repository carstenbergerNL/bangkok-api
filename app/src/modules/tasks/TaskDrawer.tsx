import { useState, useEffect } from 'react';
import {
  getTasksModuleById,
  createTasksModule,
  updateTasksModule,
  deleteTasksModule,
  setTasksModuleStatus,
} from './tasksService';
import { addToast } from '../../utils/toast';
import type { TasksStandaloneTask, CreateTasksStandaloneRequest, UpdateTasksStandaloneRequest } from './types';
import type { TenantAdminUser } from '../../services/tenantAdminService';

interface TaskDrawerProps {
  taskId: string | null;
  open: boolean;
  onClose: () => void;
  onSaved: () => void;
  onDeleted: () => void;
  canCreate: boolean;
  canEdit: boolean;
  canDelete: boolean;
  canAssign: boolean;
  assigneeOptions: TenantAdminUser[];
}

const PRIORITIES = ['Low', 'Medium', 'High'];
const STATUSES = ['Open', 'Completed'];

export function TaskDrawer({
  taskId,
  open,
  onClose,
  onSaved,
  onDeleted,
  canCreate,
  canEdit,
  canDelete,
  canAssign,
  assigneeOptions,
}: TaskDrawerProps) {
  const [task, setTask] = useState<TasksStandaloneTask | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [status, setStatus] = useState('Open');
  const [priority, setPriority] = useState('Medium');
  const [assignedToUserId, setAssignedToUserId] = useState<string | null>(null);
  const [dueDate, setDueDate] = useState('');

  const isCreate = taskId == null;
  const readOnly = !isCreate && !canEdit;

  useEffect(() => {
    if (!open) return;
    if (isCreate) {
      setTask(null);
      setTitle('');
      setDescription('');
      setStatus('Open');
      setPriority('Medium');
      setAssignedToUserId(null);
      setDueDate('');
      setLoading(false);
      return;
    }
    setLoading(true);
    getTasksModuleById(taskId!)
      .then((res) => {
        const data = res.data ?? (res as unknown as { data?: TasksStandaloneTask }).data;
        if (res.success && data) {
          setTask(data);
          setTitle(data.title);
          setDescription(data.description ?? '');
          setStatus(data.status);
          setPriority(data.priority);
          setAssignedToUserId(data.assignedToUserId);
          setDueDate(data.dueDate ? data.dueDate.slice(0, 10) : '');
        } else {
          addToast('error', res.error?.message ?? 'Task not found.');
          onClose();
        }
      })
      .catch(() => {
        addToast('error', 'Failed to load task.');
        onClose();
      })
      .finally(() => setLoading(false));
  }, [open, taskId, isCreate, onClose]);

  const handleSave = () => {
    if (!canCreate && isCreate) return;
    if (!canEdit && !isCreate) return;
    if (!title.trim()) {
      addToast('error', 'Title is required.');
      return;
    }
    setSaving(true);
    if (isCreate) {
      const req: CreateTasksStandaloneRequest = {
        title: title.trim(),
        description: description.trim() || null,
        status: 'Open',
        priority,
        assignedToUserId: canAssign ? assignedToUserId : null,
        dueDate: dueDate ? `${dueDate}T00:00:00Z` : null,
      };
      createTasksModule(req)
        .then((res) => {
          if (res.success && res.data) {
            addToast('success', 'Task created.');
            onSaved();
            onClose();
          } else {
            addToast('error', res.error?.message ?? 'Failed to create task.');
          }
        })
        .finally(() => setSaving(false));
    } else {
      const req: UpdateTasksStandaloneRequest = {
        title: title.trim(),
        description: description.trim() || null,
        status,
        priority,
        assignedToUserId: canAssign ? assignedToUserId : undefined,
        dueDate: dueDate ? `${dueDate}T00:00:00Z` : null,
      };
      updateTasksModule(taskId!, req)
        .then((res) => {
          if (res.success && res.data) {
            setTask(res.data!);
            addToast('success', 'Task updated.');
            onSaved();
          } else {
            addToast('error', res.error?.message ?? 'Failed to update task.');
          }
        })
        .finally(() => setSaving(false));
    }
  };

  const handleDelete = () => {
    if (!canDelete || !taskId) return;
    if (!window.confirm('Delete this task?')) return;
    setDeleting(true);
    deleteTasksModule(taskId)
      .then((res) => {
        if (res.success) {
          addToast('success', 'Task deleted.');
          onDeleted();
          onClose();
        } else {
          addToast('error', res.error?.message ?? 'Failed to delete task.');
        }
      })
      .finally(() => setDeleting(false));
  };

  const handleToggleComplete = () => {
    if (!taskId || !canEdit || !task) return;
    const newStatus = task.status === 'Completed' ? 'Open' : 'Completed';
    setTasksModuleStatus(taskId, newStatus).then((res) => {
      if (res.success && res.data) {
        setTask(res.data);
        setStatus(res.data.status);
        addToast('success', newStatus === 'Completed' ? 'Task completed.' : 'Task reopened.');
        onSaved();
      } else {
        addToast('error', res.error?.message ?? 'Failed to update status.');
      }
    });
  };

  if (!open) return null;

  const headerColor = 'var(--card-header-color, #323130)';
  const descColor = 'var(--card-description-color, #605e5c)';

  return (
    <>
      <div className="fixed inset-0 z-40 bg-black/20 dark:bg-black/40" onClick={onClose} aria-hidden />
      <div
        className="fixed right-0 top-0 z-50 h-full w-full max-w-lg bg-white dark:bg-slate-800 shadow-xl flex flex-col"
        role="dialog"
        aria-label={isCreate ? 'Add task' : 'Task details'}
      >
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200 dark:border-slate-600">
          <h2 className="text-lg font-semibold" style={{ color: headerColor }}>
            {isCreate ? 'Add Task' : task?.title ?? 'Task'}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-slate-700 text-gray-600 dark:text-slate-400"
            aria-label="Close"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <div className="flex-1 overflow-y-auto px-6 py-5 space-y-4">
          {loading ? (
            <div className="animate-pulse space-y-3">
              <div className="h-10 bg-gray-200 dark:bg-slate-600 rounded" />
              <div className="h-24 bg-gray-100 dark:bg-slate-700 rounded" />
            </div>
          ) : (
            <>
              <div>
                <label className="block text-sm font-medium mb-1" style={{ color: headerColor }}>Title</label>
                <input
                  type="text"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  readOnly={readOnly}
                  className="w-full px-3 py-2 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 disabled:opacity-60"
                  placeholder="Task title"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1" style={{ color: headerColor }}>Description</label>
                <textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  readOnly={readOnly}
                  rows={3}
                  className="w-full px-3 py-2 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 disabled:opacity-60 resize-none"
                  placeholder="Optional description"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1" style={{ color: headerColor }}>Status</label>
                <select
                  value={status}
                  onChange={(e) => setStatus(e.target.value)}
                  disabled={readOnly}
                  className="w-full px-3 py-2 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 disabled:opacity-60"
                >
                  {STATUSES.map((s) => (
                    <option key={s} value={s}>{s}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1" style={{ color: headerColor }}>Priority</label>
                <select
                  value={priority}
                  onChange={(e) => setPriority(e.target.value)}
                  disabled={readOnly}
                  className="w-full px-3 py-2 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 disabled:opacity-60"
                >
                  {PRIORITIES.map((p) => (
                    <option key={p} value={p}>{p}</option>
                  ))}
                </select>
              </div>
              {canAssign && (
                <div>
                  <label className="block text-sm font-medium mb-1" style={{ color: headerColor }}>Assigned to</label>
                  <select
                    value={assignedToUserId ?? ''}
                    onChange={(e) => setAssignedToUserId(e.target.value || null)}
                    disabled={readOnly}
                    className="w-full px-3 py-2 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 disabled:opacity-60"
                  >
                    <option value="">Unassigned</option>
                    {assigneeOptions.map((u) => (
                      <option key={u.userId} value={u.userId}>
                        {u.displayName || u.email}
                      </option>
                    ))}
                  </select>
                </div>
              )}
              <div>
                <label className="block text-sm font-medium mb-1" style={{ color: headerColor }}>Due date</label>
                <input
                  type="date"
                  value={dueDate}
                  onChange={(e) => setDueDate(e.target.value)}
                  readOnly={readOnly}
                  className="w-full px-3 py-2 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 disabled:opacity-60"
                />
              </div>
              {!isCreate && canEdit && task && (
                <div className="pt-2">
                  <button
                    type="button"
                    onClick={handleToggleComplete}
                    className="text-sm font-medium text-blue-600 dark:text-blue-400 hover:underline"
                  >
                    {task.status === 'Completed' ? 'Reopen task' : 'Mark complete'}
                  </button>
                </div>
              )}
            </>
          )}
        </div>

        <div className="flex items-center justify-between gap-3 px-6 py-4 border-t border-gray-200 dark:border-slate-600">
          <div>
            {!isCreate && canDelete && (
              <button
                type="button"
                onClick={handleDelete}
                disabled={deleting}
                className="text-sm text-red-600 dark:text-red-400 hover:underline disabled:opacity-50"
              >
                {deleting ? 'Deleting…' : 'Delete'}
              </button>
            )}
          </div>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-lg border border-gray-200 dark:border-slate-600 text-sm font-medium hover:bg-gray-50 dark:hover:bg-slate-700"
            >
              Cancel
            </button>
            {(isCreate ? canCreate : canEdit) && (
              <button
                type="button"
                onClick={handleSave}
                disabled={saving || !title.trim()}
                className="px-4 py-2 rounded-lg bg-blue-600 text-white text-sm font-medium hover:bg-blue-700 disabled:opacity-50"
              >
                {saving ? 'Saving…' : isCreate ? 'Create' : 'Save'}
              </button>
            )}
          </div>
        </div>
      </div>
    </>
  );
}
