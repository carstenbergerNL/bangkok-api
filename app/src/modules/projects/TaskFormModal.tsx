import { useState, useEffect, useCallback } from 'react';
import { FormSidebar } from '../../components/FormSidebar';
import { addToast } from '../../utils/toast';
import { getUsers } from '../../services/userService';
import { getLabels } from './labelService';
import { getCustomFields } from './customFieldService';
import type { User } from '../../models/User';
import { TASK_STATUSES, TASK_PRIORITIES, RECURRENCE_PATTERNS } from './types';
import type { Task, CreateTaskRequest, UpdateTaskRequest, Label, ProjectCustomField } from './types';

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
  const [isRecurring, setIsRecurring] = useState(false);
  const [recurrencePattern, setRecurrencePattern] = useState<string>('Weekly');
  const [recurrenceInterval, setRecurrenceInterval] = useState<string>('1');
  const [recurrenceEndDate, setRecurrenceEndDate] = useState('');
  const [selectedLabelIds, setSelectedLabelIds] = useState<string[]>([]);
  const [projectLabels, setProjectLabels] = useState<Label[]>([]);
  const [customFields, setCustomFields] = useState<ProjectCustomField[]>([]);
  const [customFieldValues, setCustomFieldValues] = useState<Record<string, string>>({});
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
      getLabels(projectId).then((res) => {
        const data = res.data ?? (res as unknown as { Data?: Label[] }).Data;
        setProjectLabels(Array.isArray(data) ? data : []);
      });
      getCustomFields(projectId).then((res) => {
        const data = res.data ?? (res as unknown as { Data?: ProjectCustomField[] }).Data;
        setCustomFields(Array.isArray(data) ? data : []);
      });
      setTitle(task?.title ?? '');
      setDescription(task?.description ?? '');
      setStatus(task?.status ?? 'ToDo');
      setPriority(task?.priority ?? 'Medium');
      setAssignedToUserId(task?.assignedToUserId ?? '');
      setDueDate(task?.dueDate ? task.dueDate.slice(0, 10) : '');
      setIsRecurring(task?.isRecurring ?? false);
      setRecurrencePattern(task?.recurrencePattern ?? 'Weekly');
      setRecurrenceInterval(task?.recurrenceInterval != null ? String(task.recurrenceInterval) : '1');
      setRecurrenceEndDate(task?.recurrenceEndDate ? task.recurrenceEndDate.slice(0, 10) : '');
      setSelectedLabelIds(task?.labels?.map((l) => l.id) ?? []);
      const cfMap: Record<string, string> = {};
      (task?.customFieldValues ?? []).forEach((v) => { cfMap[v.fieldId] = v.value ?? ''; });
      setCustomFieldValues(cfMap);
    }
  }, [open, task, projectId, loadUsers]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = title.trim();
    if (!trimmed) return;
    setSaving(true);
    try {
      const intervalNum = Math.max(1, Math.min(999, parseInt(recurrenceInterval, 10) || 1));
      const recurrencePayload = isRecurring
        ? {
            isRecurring: true,
            recurrencePattern: recurrencePattern || 'Weekly',
            recurrenceInterval: intervalNum,
            recurrenceEndDate: recurrenceEndDate || null,
          }
        : { isRecurring: false, recurrencePattern: null, recurrenceInterval: null, recurrenceEndDate: null };
      const cfPayload = customFields.length > 0
        ? { customFieldValues: customFields.map((f) => ({ fieldId: f.id, value: (customFieldValues[f.id] ?? '').trim() || null })) }
        : {};
      const res = isEdit
        ? await save(task!.id, {
            title: trimmed,
            description: description.trim() || null,
            status,
            priority,
            assignedToUserId: canAssign && assignedToUserId ? assignedToUserId : undefined,
            dueDate: dueDate || null,
            ...recurrencePayload,
            labelIds: selectedLabelIds.length > 0 ? selectedLabelIds : undefined,
            ...cfPayload,
          })
        : await create({
            projectId,
            title: trimmed,
            description: description.trim() || null,
            status,
            priority,
            assignedToUserId: canAssign && assignedToUserId ? assignedToUserId : null,
            dueDate: dueDate || null,
            ...recurrencePayload,
            labelIds: selectedLabelIds.length > 0 ? selectedLabelIds : undefined,
            ...cfPayload,
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

  const handleLabelToggle = (labelId: string) => {
    setSelectedLabelIds((prev) =>
      prev.includes(labelId) ? prev.filter((id) => id !== labelId) : [...prev, labelId]
    );
  };

  return (
    <FormSidebar open={open} onClose={onClose} title={isEdit ? 'Edit task' : 'Add task'} width="wide">
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
        <div className="border-t border-gray-200 dark:border-slate-600 pt-4 space-y-3">
          <div className="flex items-center gap-2">
            <input
              id="task-recurring"
              type="checkbox"
              checked={isRecurring}
              onChange={(e) => setIsRecurring(e.target.checked)}
              className="rounded border-gray-300 dark:border-slate-600 text-blue-600 focus:ring-blue-500"
            />
            <label htmlFor="task-recurring" className="text-sm font-medium" style={{ color: 'var(--card-header-color, #323130)' }}>
              Recurring task
            </label>
          </div>
          {isRecurring && (
            <div className="grid grid-cols-2 gap-3 pl-6">
              <div>
                <label htmlFor="task-recurrence-pattern" className="block text-xs font-medium mb-0.5" style={{ color: 'var(--card-header-color, #323130)' }}>
                  Pattern
                </label>
                <select
                  id="task-recurrence-pattern"
                  value={recurrencePattern}
                  onChange={(e) => setRecurrencePattern(e.target.value)}
                  className="w-full px-2 py-1.5 text-sm rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600"
                >
                  {RECURRENCE_PATTERNS.map((p) => (
                    <option key={p} value={p}>{p}</option>
                  ))}
                </select>
              </div>
              <div>
                <label htmlFor="task-recurrence-interval" className="block text-xs font-medium mb-0.5" style={{ color: 'var(--card-header-color, #323130)' }}>
                  Every
                </label>
                <input
                  id="task-recurrence-interval"
                  type="number"
                  min={1}
                  max={999}
                  value={recurrenceInterval}
                  onChange={(e) => setRecurrenceInterval(e.target.value)}
                  className="w-full px-2 py-1.5 text-sm rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600"
                />
              </div>
              <div className="col-span-2">
                <label htmlFor="task-recurrence-end" className="block text-xs font-medium mb-0.5" style={{ color: 'var(--card-header-color, #323130)' }}>
                  End date (optional)
                </label>
                <input
                  id="task-recurrence-end"
                  type="date"
                  value={recurrenceEndDate}
                  onChange={(e) => setRecurrenceEndDate(e.target.value)}
                  className="w-full px-2 py-1.5 text-sm rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600"
                />
              </div>
            </div>
          )}
        </div>
        {projectLabels.length > 0 && (
          <div>
            <span className="block text-sm font-medium mb-2" style={{ color: 'var(--card-header-color, #323130)' }}>
              Labels
            </span>
            <div className="flex flex-wrap gap-2">
              {projectLabels.map((label) => {
                const isSelected = selectedLabelIds.includes(label.id);
                return (
                  <label
                    key={label.id}
                    className="inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg text-xs font-medium border border-gray-200 dark:border-slate-600 cursor-pointer hover:opacity-90 transition-colors"
                    style={{ backgroundColor: isSelected ? label.color : 'var(--dropdown-bg, #ffffff)', color: isSelected ? (label.color === '#ffffff' || label.color === '#fff' ? '#333' : '#fff') : undefined }}
                  >
                    <input
                      type="checkbox"
                      checked={isSelected}
                      onChange={() => handleLabelToggle(label.id)}
                      className="sr-only"
                    />
                    {label.name}
                  </label>
                );
              })}
            </div>
          </div>
        )}
        {customFields.length > 0 && (
          <div className="space-y-3 border-t border-gray-200 dark:border-slate-600 pt-4">
            <span className="block text-sm font-medium" style={{ color: 'var(--card-header-color, #323130)' }}>
              Custom fields
            </span>
            {customFields.map((field) => {
              const value = customFieldValues[field.id] ?? '';
              const inputClass = 'w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 text-sm';
              const ft = (field.fieldType || 'Text').toLowerCase();
              if (ft === 'number') {
                return (
                  <div key={field.id}>
                    <label htmlFor={`task-cf-${field.id}`} className="block text-xs font-medium mb-0.5" style={{ color: 'var(--card-description-color, #605e5c)' }}>{field.name}</label>
                    <input id={`task-cf-${field.id}`} type="number" value={value} onChange={(e) => setCustomFieldValues((p) => ({ ...p, [field.id]: e.target.value }))} className={inputClass} />
                  </div>
                );
              }
              if (ft === 'date') {
                return (
                  <div key={field.id}>
                    <label htmlFor={`task-cf-${field.id}`} className="block text-xs font-medium mb-0.5" style={{ color: 'var(--card-description-color, #605e5c)' }}>{field.name}</label>
                    <input id={`task-cf-${field.id}`} type="date" value={value} onChange={(e) => setCustomFieldValues((p) => ({ ...p, [field.id]: e.target.value }))} className={inputClass} />
                  </div>
                );
              }
              if (ft === 'dropdown' && field.options) {
                const opts = field.options.split(',').map((o) => o.trim()).filter(Boolean);
                return (
                  <div key={field.id}>
                    <label htmlFor={`task-cf-${field.id}`} className="block text-xs font-medium mb-0.5" style={{ color: 'var(--card-description-color, #605e5c)' }}>{field.name}</label>
                    <select id={`task-cf-${field.id}`} value={value} onChange={(e) => setCustomFieldValues((p) => ({ ...p, [field.id]: e.target.value }))} className={inputClass}>
                      <option value="">—</option>
                      {opts.map((opt) => <option key={opt} value={opt}>{opt}</option>)}
                    </select>
                  </div>
                );
              }
              return (
                <div key={field.id}>
                  <label htmlFor={`task-cf-${field.id}`} className="block text-xs font-medium mb-0.5" style={{ color: 'var(--card-description-color, #605e5c)' }}>{field.name}</label>
                  <input id={`task-cf-${field.id}`} type="text" value={value} onChange={(e) => setCustomFieldValues((p) => ({ ...p, [field.id]: e.target.value }))} className={inputClass} placeholder="Optional" />
                </div>
              );
            })}
          </div>
        )}
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="px-4 py-2 rounded-lg border border-gray-200 dark:border-slate-600 hover:bg-gray-50 dark:hover:bg-slate-700 transition-colors">
            Cancel
          </button>
          <button type="submit" disabled={saving || !title.trim()} className="px-4 py-2 rounded-lg bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50 transition-colors">
            {saving ? 'Saving…' : 'Save'}
          </button>
        </div>
      </form>
    </FormSidebar>
  );
}
