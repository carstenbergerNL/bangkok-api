import { useCallback, useEffect, useState } from 'react';
import { getProjectTemplates, createProjectTemplate, updateProjectTemplate, deleteProjectTemplate } from '../../modules/projects/projectService';
import type { ProjectTemplate, CreateProjectTemplateRequest, CreateProjectTemplateTaskRequest, UpdateProjectTemplateRequest } from '../../modules/projects/types';
import { TASK_STATUSES, TASK_PRIORITIES } from '../../modules/projects/types';
import { addToast } from '../../utils/toast';
import { FormSidebar } from '../../components/FormSidebar';
import { TableSkeleton } from '../../components/TableSkeleton';
import { Modal } from '../../components/Modal';

export function TemplatesSection() {
  const [templates, setTemplates] = useState<ProjectTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [editTarget, setEditTarget] = useState<ProjectTemplate | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ProjectTemplate | null>(null);

  const loadTemplates = useCallback(() => {
    setLoading(true);
    setError(null);
    getProjectTemplates()
      .then((res) => {
        const data = res.data ?? (res as unknown as { Data?: ProjectTemplate[] }).Data;
        if (res.success && Array.isArray(data)) setTemplates(data);
        else setError(res.error?.message ?? res.message ?? 'Failed to load templates.');
      })
      .catch(() => {
        setError('Network or server error.');
        addToast('error', 'Failed to load templates.');
      })
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    loadTemplates();
  }, [loadTemplates]);

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <p className="text-sm text-gray-600 dark:text-gray-400">
          Create and manage project templates. Use them when creating a new project to pre-fill tasks.
        </p>
        <button
          type="button"
          onClick={() => setCreateOpen(true)}
          className="btn-primary shrink-0"
        >
          + Create Template
        </button>
      </div>

      <div className="rounded-lg border border-gray-200 dark:border-[#2d3d5c] overflow-hidden bg-gray-50/50 dark:bg-blue-900/20">
        {error && !loading && <div className="p-4"><div className="alert-error">{error}</div></div>}
        {loading && <div className="p-6"><TableSkeleton rows={6} cols={4} /></div>}
        {!loading && !error && templates.length === 0 && (
          <div className="p-12 text-center text-gray-500 dark:text-gray-400 text-sm">
            No templates yet. Create one to get started.
          </div>
        )}
        {!loading && !error && templates.length > 0 && (
          <div className="overflow-x-auto">
            <table className="table-grid">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Description</th>
                  <th>Tasks</th>
                  <th className="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {templates.map((t) => (
                  <tr key={t.id}>
                    <td className="font-medium text-gray-900 dark:text-white">{t.name}</td>
                    <td className="muted">{t.description ?? '—'}</td>
                    <td>{(t.tasks ?? []).length}</td>
                    <td className="text-right">
                      <div className="table-actions">
                        <button
                          type="button"
                          onClick={() => setEditTarget(t)}
                          className="table-link inline-flex items-center gap-1.5"
                        >
                          <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                          </svg>
                          Edit
                        </button>
                        <button
                          type="button"
                          onClick={() => setDeleteTarget(t)}
                          className="table-link-danger inline-flex items-center gap-1.5"
                        >
                          <svg className="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                          </svg>
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {createOpen && (
        <CreateTemplateSidebar
          onClose={() => setCreateOpen(false)}
          onSuccess={() => {
            setCreateOpen(false);
            loadTemplates();
            addToast('success', 'Template created.');
          }}
        />
      )}

      {editTarget && (
        <EditTemplateSidebar
          template={editTarget}
          onClose={() => setEditTarget(null)}
          onSuccess={() => {
            setEditTarget(null);
            loadTemplates();
            addToast('success', 'Template updated.');
          }}
        />
      )}

      {deleteTarget && (
        <Modal open title="Delete template" onClose={() => setDeleteTarget(null)}>
          <div className="space-y-4">
            <p>Are you sure you want to delete the template &quot;{deleteTarget.name}&quot;? This cannot be undone.</p>
            <div className="flex justify-end gap-2">
              <button type="button" onClick={() => setDeleteTarget(null)} className="btn-secondary">Cancel</button>
              <button
                type="button"
                onClick={() => {
                  deleteProjectTemplate(deleteTarget.id).then((res) => {
                    if (res.success) {
                      setDeleteTarget(null);
                      loadTemplates();
                      addToast('success', 'Template deleted.');
                    } else {
                      addToast('error', res.error?.message ?? res.message ?? 'Failed to delete.');
                    }
                  });
                }}
                className="btn-danger"
              >
                Delete
              </button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
}

interface CreateTemplateSidebarProps {
  onClose: () => void;
  onSuccess: () => void;
}

function CreateTemplateSidebar({ onClose, onSuccess }: CreateTemplateSidebarProps) {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [tasks, setTasks] = useState<CreateProjectTemplateTaskRequest[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const addTask = () => {
    setTasks((prev) => [...prev, { title: '', defaultStatus: 'ToDo', defaultPriority: 'Medium' }]);
  };

  const removeTask = (index: number) => {
    setTasks((prev) => prev.filter((_, i) => i !== index));
  };

  const updateTask = (index: number, field: keyof CreateProjectTemplateTaskRequest, value: string | null) => {
    setTasks((prev) => {
      const next = [...prev];
      next[index] = { ...next[index], [field]: value ?? undefined };
      return next;
    });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    if (!name.trim()) {
      setFormError('Name is required.');
      return;
    }
    const taskList = tasks
      .map((t) => ({ ...t, title: t.title?.trim() }))
      .filter((t) => t.title);
    const payload: CreateProjectTemplateRequest = {
      name: name.trim(),
      description: description.trim() || undefined,
      tasks: taskList.length ? taskList : undefined,
    };
    setSubmitting(true);
    createProjectTemplate(payload)
      .then((res) => {
        if (res.success) onSuccess();
        else setFormError(res.error?.message ?? res.message ?? 'Failed to create template.');
      })
      .catch(() => setFormError('Network or server error.'))
      .finally(() => setSubmitting(false));
  };

  return (
    <FormSidebar open title="Create Template" onClose={onClose} width="wide">
      <form onSubmit={handleSubmit} className="space-y-4 flex flex-col h-full">
        <div className="flex-1 overflow-y-auto space-y-4 pr-2">
          {formError && <div className="alert-error">{formError}</div>}
          <div className="form-group">
            <label htmlFor="tpl-name" className="input-label">Name *</label>
            <input
              id="tpl-name"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="input"
              required
              maxLength={200}
            />
          </div>
          <div className="form-group">
            <label htmlFor="tpl-desc" className="input-label">Description</label>
            <textarea
              id="tpl-desc"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="input min-h-[80px]"
              maxLength={500}
              rows={3}
            />
          </div>

          <div className="flex items-center justify-between">
            <span className="input-label">Template tasks (optional)</span>
            <button type="button" onClick={addTask} className="btn-secondary text-sm">
              + Add task
            </button>
          </div>
          {tasks.length > 0 && (
            <div className="space-y-3 border border-gray-200 dark:border-[#2d3d5c] rounded-lg p-3 bg-white dark:bg-slate-800/50">
              {tasks.map((task, i) => (
                <div key={i} className="p-3 rounded border border-gray-100 dark:border-slate-700 space-y-2">
                  <div className="flex justify-between items-center gap-2">
                    <span className="text-sm font-medium text-gray-700 dark:text-gray-300">Task {i + 1}</span>
                    <button type="button" onClick={() => removeTask(i)} className="text-red-600 hover:underline text-sm">
                      Remove
                    </button>
                  </div>
                  <input
                    type="text"
                    placeholder="Title"
                    value={task.title ?? ''}
                    onChange={(e) => updateTask(i, 'title', e.target.value)}
                    className="input text-sm"
                    maxLength={200}
                  />
                  <textarea
                    placeholder="Description"
                    value={task.description ?? ''}
                    onChange={(e) => updateTask(i, 'description', e.target.value || null)}
                    className="input text-sm min-h-[60px]"
                    maxLength={1000}
                    rows={2}
                  />
                  <div className="grid grid-cols-2 gap-2">
                    <div>
                      <label className="block text-xs text-gray-500 dark:text-gray-400 mb-0.5">Default status</label>
                      <select
                        value={task.defaultStatus ?? 'ToDo'}
                        onChange={(e) => updateTask(i, 'defaultStatus', e.target.value)}
                        className="input text-sm w-full"
                      >
                        {TASK_STATUSES.map((s) => (
                          <option key={s} value={s}>{s}</option>
                        ))}
                      </select>
                    </div>
                    <div>
                      <label className="block text-xs text-gray-500 dark:text-gray-400 mb-0.5">Default priority</label>
                      <select
                        value={task.defaultPriority ?? 'Medium'}
                        onChange={(e) => updateTask(i, 'defaultPriority', e.target.value)}
                        className="input text-sm w-full"
                      >
                        {TASK_PRIORITIES.map((p) => (
                          <option key={p} value={p}>{p}</option>
                        ))}
                      </select>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
        <div className="flex justify-end gap-2 pt-2 border-t border-gray-200 dark:border-[#2d3d5c] shrink-0">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="submit" disabled={submitting} className="btn-primary">
            {submitting ? 'Creating…' : 'Create'}
          </button>
        </div>
      </form>
    </FormSidebar>
  );
}

interface EditTemplateSidebarProps {
  template: ProjectTemplate;
  onClose: () => void;
  onSuccess: () => void;
}

function EditTemplateSidebar({ template, onClose, onSuccess }: EditTemplateSidebarProps) {
  const [name, setName] = useState(template.name);
  const [description, setDescription] = useState(template.description ?? '');
  const [tasks, setTasks] = useState<CreateProjectTemplateTaskRequest[]>(
    (template.tasks ?? []).map((t) => ({
      title: t.title,
      description: t.description ?? undefined,
      defaultStatus: t.defaultStatus ?? 'ToDo',
      defaultPriority: t.defaultPriority ?? 'Medium',
    }))
  );
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const addTask = () => {
    setTasks((prev) => [...prev, { title: '', defaultStatus: 'ToDo', defaultPriority: 'Medium' }]);
  };

  const removeTask = (index: number) => {
    setTasks((prev) => prev.filter((_, i) => i !== index));
  };

  const updateTask = (index: number, field: keyof CreateProjectTemplateTaskRequest, value: string | null) => {
    setTasks((prev) => {
      const next = [...prev];
      next[index] = { ...next[index], [field]: value ?? undefined };
      return next;
    });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    if (!name.trim()) {
      setFormError('Name is required.');
      return;
    }
    const taskList = tasks
      .map((t) => ({ ...t, title: t.title?.trim() }))
      .filter((t) => t.title);
    const payload: UpdateProjectTemplateRequest = {
      name: name.trim(),
      description: description.trim() || undefined,
      tasks: taskList.length ? taskList : undefined,
    };
    setSubmitting(true);
    updateProjectTemplate(template.id, payload)
      .then((res) => {
        if (res.success) onSuccess();
        else setFormError(res.error?.message ?? res.message ?? 'Failed to update template.');
      })
      .catch(() => setFormError('Network or server error.'))
      .finally(() => setSubmitting(false));
  };

  return (
    <FormSidebar open title="Edit Template" onClose={onClose} width="wide">
      <form onSubmit={handleSubmit} className="space-y-4 flex flex-col h-full">
        <div className="flex-1 overflow-y-auto space-y-4 pr-2">
          {formError && <div className="alert-error">{formError}</div>}
          <div className="form-group">
            <label htmlFor="edit-tpl-name" className="input-label">Name *</label>
            <input
              id="edit-tpl-name"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="input"
              required
              maxLength={200}
            />
          </div>
          <div className="form-group">
            <label htmlFor="edit-tpl-desc" className="input-label">Description</label>
            <textarea
              id="edit-tpl-desc"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="input min-h-[80px]"
              maxLength={500}
              rows={3}
            />
          </div>

          <div className="flex items-center justify-between">
            <span className="input-label">Template tasks (optional)</span>
            <button type="button" onClick={addTask} className="btn-secondary text-sm">
              + Add task
            </button>
          </div>
          {tasks.length > 0 && (
            <div className="space-y-3 border border-gray-200 dark:border-[#2d3d5c] rounded-lg p-3 bg-white dark:bg-slate-800/50">
              {tasks.map((task, i) => (
                <div key={i} className="p-3 rounded border border-gray-100 dark:border-slate-700 space-y-2">
                  <div className="flex justify-between items-center gap-2">
                    <span className="text-sm font-medium text-gray-700 dark:text-gray-300">Task {i + 1}</span>
                    <button type="button" onClick={() => removeTask(i)} className="text-red-600 hover:underline text-sm">
                      Remove
                    </button>
                  </div>
                  <input
                    type="text"
                    placeholder="Title"
                    value={task.title ?? ''}
                    onChange={(e) => updateTask(i, 'title', e.target.value)}
                    className="input text-sm"
                    maxLength={200}
                  />
                  <textarea
                    placeholder="Description"
                    value={task.description ?? ''}
                    onChange={(e) => updateTask(i, 'description', e.target.value || null)}
                    className="input text-sm min-h-[60px]"
                    maxLength={1000}
                    rows={2}
                  />
                  <div className="grid grid-cols-2 gap-2">
                    <div>
                      <label className="block text-xs text-gray-500 dark:text-gray-400 mb-0.5">Default status</label>
                      <select
                        value={task.defaultStatus ?? 'ToDo'}
                        onChange={(e) => updateTask(i, 'defaultStatus', e.target.value)}
                        className="input text-sm w-full"
                      >
                        {TASK_STATUSES.map((s) => (
                          <option key={s} value={s}>{s}</option>
                        ))}
                      </select>
                    </div>
                    <div>
                      <label className="block text-xs text-gray-500 dark:text-gray-400 mb-0.5">Default priority</label>
                      <select
                        value={task.defaultPriority ?? 'Medium'}
                        onChange={(e) => updateTask(i, 'defaultPriority', e.target.value)}
                        className="input text-sm w-full"
                      >
                        {TASK_PRIORITIES.map((p) => (
                          <option key={p} value={p}>{p}</option>
                        ))}
                      </select>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
        <div className="flex justify-end gap-2 pt-2 border-t border-gray-200 dark:border-[#2d3d5c] shrink-0">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="submit" disabled={submitting} className="btn-primary">
            {submitting ? 'Saving…' : 'Save'}
          </button>
        </div>
      </form>
    </FormSidebar>
  );
}
