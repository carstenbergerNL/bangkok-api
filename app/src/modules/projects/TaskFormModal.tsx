import { useState, useEffect, useCallback } from 'react';
import { Modal } from '../../components/Modal';
import { addToast } from '../../utils/toast';
import { getUsers } from '../../services/userService';
import type { User } from '../../models/User';
import { TASK_STATUSES, TASK_PRIORITIES } from './types';
import type { Task, CreateTaskRequest, UpdateTaskRequest } from './types';

interface TaskFormModalProps {
  open: boolean;
  onClose: () => void;
  onSaved: () => void;
  projectId: string;
  task: Task | null;
  save: (id: string, data: UpdateTaskRequest) => Promise<{ success: boolean; error?: { message?: string } }>;
  create: (data: CreateTaskRequest) => Promise<{ success: boolean; error?: { message?: string } }>;
  canAssign: boolean;
}

export function TaskFormModal(props: TaskFormModalProps) {
  const { open, onClose, onSaved, projectId, task, save, create, canAssign } = props;
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [status, setStatus] = useState('ToDo');
  const [priority, setPriority] = useState('Medium');
  const [assignedToUserId, setAssignedToUserId] = useState('');
  const [dueDate, setDueDate] = useState('');
  const [users, setUsers] = useState<User[]>([]);
  const [saving, setSaving] = useState(false);
  const isEdit = !!task;

  const loadUsers = useCallback(() => {
    getUsers(1, 500, false).then((res) => {
      const data = res.data ?? (res as unknown as { Data?: { items?: User[] } }).Data;
      const items = data?.items ?? [];
      setUsers(Array.isArray(items) ? items.filter((u) => !u.isDeleted) : []);
    });
  }, []);

  useEffect(() => {
    if (open) {
      loadUsers();
      setTitle(task?.title ?? '');
      setDescription(task?.description ?? '');
      setStatus(task?.status ?? 'ToDo');
      setPriority(task?.priority ?? 'Medium');
      setAssignedToUserId(task?.assignedToUserId ?? '');
      setDueDate(task?.dueDate ? task.dueDate.slice(0, 10) : '');
    }
  }, [open, task, loadUsers]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = title.trim();
    if (!trimmed) return;
    setSaving(true);
    try {
      const res = isEdit
        ? await save(task!.id, {
            title: trimmed,
            description: description.trim() || null,
            status,
            priority,
            assignedToUserId: canAssign && assignedToUserId ? assignedToUserId : undefined,
            dueDate: dueDate || null,
          })
        : await create({
            projectId,
            title: trimmed,
            description: description.trim() || null,
            status,
            priority,
            assignedToUserId: canAssign && assignedToUserId ? assignedToUserId : null,
            dueDate: dueDate || null,
          });
      if (res.success) {
        addToast('success', isEdit ? 'Task updated.' : 'Task created.');
        onSaved();
        onClose();
      } else {
        addToast('error', res.error?.message ?? 'Failed to save task.');
      }
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal open={open} onClose={onClose} title={isEdit ? 'Edit task' : 'Add task'}>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label htmlFor="task-title" className="block text-sm font-medium mb-1" style={{ color: 'var(--card-header-color, #323130)' }}>
            Title <span className="text-red-500">*</span>
          </label>
          <input
            id="task-title"
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            placeholder="Task title"
            required
            autoFocus
          />
        </div>
        <div>
          <label htmlFor="task-desc" className="block text-sm font-medium mb-1" style={{ color: 'var(--card-header-color, #323130)' }}>
            Description
          </label>
          <textarea
            id="task-desc"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
            placeholder="Description"
            rows={3}
          />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label htmlFor="task-status" className="block text-sm font-medium mb-1" style={{ color: 'var(--card-header-color, #323130)' }}>
              Status
            </label>
            <select
              id="task-status"
              value={status}
              onChange={(e) => setStatus(e.target.value)}
              className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            >
              {TASK_STATUSES.map((s) => (
                <option key={s} value={s}>{s === 'ToDo' ? 'Todo' : s === 'InProgress' ? 'In Progress' : s}</option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="task-priority" className="block text-sm font-medium mb-1" style={{ color: 'var(--card-header-color, #323130)' }}>
              Priority
            </label>
            <select
              id="task-priority"
              value={priority}
              onChange={(e) => setPriority(e.target.value)}
              className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            >
              {TASK_PRIORITIES.map((p) => (
                <option key={p} value={p}>{p}</option>
              ))}
            </select>
          </div>
        </div>
        {canAssign && (
          <div>
            <label htmlFor="task-assignee" className="block text-sm font-medium mb-1" style={{ color: 'var(--card-header-color, #323130)' }}>
              Assigned to
            </label>
            <select
              id="task-assignee"
              value={assignedToUserId}
              onChange={(e) => setAssignedToUserId(e.target.value)}
              className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            >
              <option value="">Unassigned</option>
              {users.map((u) => (
                <option key={u.id} value={u.id}>{u.displayName || u.email}</option>
              ))}
            </select>
          </div>
        )}
        <div>
          <label htmlFor="task-due" className="block text-sm font-medium mb-1" style={{ color: 'var(--card-header-color, #323130)' }}>
            Due date
          </label>
          <input
            id="task-due"
            type="date"
            value={dueDate}
            onChange={(e) => setDueDate(e.target.value)}
            className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          />
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="px-4 py-2 rounded-lg border border-gray-200 dark:border-slate-600 hover:bg-gray-50 dark:hover:bg-slate-700 transition-colors">
            Cancel
          </button>
          <button type="submit" disabled={saving || !title.trim()} className="px-4 py-2 rounded-lg bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50 transition-colors">
            {saving ? 'Saving…' : 'Save'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
