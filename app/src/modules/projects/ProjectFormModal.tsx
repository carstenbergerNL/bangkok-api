import { useState, useEffect } from 'react';
import { FormSidebar } from '../../components/FormSidebar';
import { addToast } from '../../utils/toast';
import { PROJECT_STATUSES } from './types';
import type { Project, CreateProjectRequest, UpdateProjectRequest } from './types';

interface ProjectFormModalProps {
  open: boolean;
  onClose: () => void;
  onSaved: () => void;
  project: Project | null;
  save: (id: string, data: UpdateProjectRequest) => Promise<{ success: boolean; error?: { message?: string } }>;
  create: (data: CreateProjectRequest) => Promise<{ success: boolean; error?: { message?: string } }>;
}

export function ProjectFormModal({ open, onClose, onSaved, project, save, create }: ProjectFormModalProps) {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [status, setStatus] = useState<string>('Active');
  const [saving, setSaving] = useState(false);

  const isEdit = !!project;

  useEffect(() => {
    if (open) {
      setName(project?.name ?? '');
      setDescription(project?.description ?? '');
      setStatus(project?.status ?? 'Active');
    }
  }, [open, project]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = name.trim();
    if (!trimmed) return;
    setSaving(true);
    try {
      const res = isEdit
        ? await save(project!.id, { name: trimmed, description: description.trim() || null, status })
        : await create({ name: trimmed, description: description.trim() || null, status });
      if (res.success) {
        addToast('success', isEdit ? 'Project updated.' : 'Project created.');
        onSaved();
        onClose();
      } else {
        addToast('error', res.error?.message ?? 'Failed to save project.');
      }
    } finally {
      setSaving(false);
    }
  };

  return (
    <FormSidebar open={open} onClose={onClose} title={isEdit ? 'Edit project' : 'Create project'}>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label htmlFor="project-name" className="block text-sm font-medium mb-1" style={{ color: 'var(--card-header-color, #323130)' }}>
            Name <span className="text-red-500">*</span>
          </label>
          <input
            id="project-name"
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-shadow"
            placeholder="Project name"
            required
            autoFocus
          />
        </div>
        <div>
          <label htmlFor="project-desc" className="block text-sm font-medium mb-1" style={{ color: 'var(--card-header-color, #323130)' }}>
            Description
          </label>
          <textarea
            id="project-desc"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-shadow resize-none"
            placeholder="Brief description"
            rows={3}
          />
        </div>
        <div>
          <label htmlFor="project-status" className="block text-sm font-medium mb-1" style={{ color: 'var(--card-header-color, #323130)' }}>
            Status
          </label>
          <select
            id="project-status"
            value={status}
            onChange={(e) => setStatus(e.target.value)}
            className="w-full px-3 py-2 rounded-lg border bg-white dark:bg-slate-800 border-gray-200 dark:border-slate-600 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          >
            {PROJECT_STATUSES.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="px-4 py-2 rounded-lg border border-gray-200 dark:border-slate-600 hover:bg-gray-50 dark:hover:bg-slate-700 transition-colors">
            Cancel
          </button>
          <button type="submit" disabled={saving || !name.trim()} className="px-4 py-2 rounded-lg bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50 transition-colors">
            {saving ? 'Saving…' : 'Save'}
          </button>
        </div>
      </form>
    </FormSidebar>
  );
}
