import { useCallback, useEffect, useState } from 'react';
import { getLabels, createLabel, deleteLabel } from './labelService';
import { addToast } from '../../utils/toast';
import type { Label, CreateLabelRequest } from './types';

const PRESET_COLORS = ['#6366f1', '#ec4899', '#10b981', '#f59e0b', '#3b82f6', '#8b5cf6', '#ef4444', '#6b7280'];

interface ProjectLabelsSettingsProps {
  projectId: string;
}

export function ProjectLabelsSettings({ projectId }: ProjectLabelsSettingsProps) {
  const [labels, setLabels] = useState<Label[]>([]);
  const [loading, setLoading] = useState(true);
  const [name, setName] = useState('');
  const [color, setColor] = useState(PRESET_COLORS[0]);
  const [saving, setSaving] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const loadLabels = useCallback(() => {
    setLoading(true);
    getLabels(projectId)
      .then((res) => {
        const data = res.data ?? (res as unknown as { Data?: Label[] }).Data;
        if (res.success && Array.isArray(data)) setLabels(data);
        else addToast('error', res.error?.message ?? 'Failed to load labels.');
      })
      .finally(() => setLoading(false));
  }, [projectId]);

  useEffect(() => {
    loadLabels();
  }, [loadLabels]);

  const handleAdd = async (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = name.trim();
    if (!trimmed) return;
    setSaving(true);
    try {
      const request: CreateLabelRequest = { name: trimmed, color };
      const res = await createLabel(projectId, request);
      if (res.success && res.data) {
        setLabels((prev) => [...prev, res.data!]);
        setName('');
        setColor(PRESET_COLORS[0]);
        addToast('success', 'Label added.');
      } else {
        addToast('error', res.error?.message ?? 'Failed to add label.');
      }
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (labelId: string) => {
    setDeletingId(labelId);
    try {
      const res = await deleteLabel(projectId, labelId);
      if (res.success) {
        setLabels((prev) => prev.filter((l) => l.id !== labelId));
        addToast('success', 'Label removed.');
      } else {
        addToast('error', res.error?.message ?? 'Failed to remove label.');
      }
    } finally {
      setDeletingId(null);
    }
  };

  if (loading) {
    return (
      <div className="animate-pulse space-y-3">
        <div className="h-6 w-48 bg-gray-200 dark:bg-slate-600 rounded" />
        <div className="h-12 bg-gray-100 dark:bg-slate-700 rounded-xl" />
        <div className="h-12 bg-gray-100 dark:bg-slate-700 rounded-xl" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold mb-1" style={{ color: 'var(--card-header-color, #323130)' }}>
          Manage Labels
        </h2>
        <p className="text-sm mb-4" style={{ color: 'var(--card-description-color, #605e5c)' }}>
          Labels can be applied to tasks. Only project members with edit access can manage them.
        </p>
      </div>

      <form onSubmit={handleAdd} className="flex flex-wrap items-end gap-3 p-4 rounded-xl border border-gray-100 dark:border-slate-700/50 bg-gray-50/50 dark:bg-slate-800/30">
        <div className="flex-1 min-w-[120px]">
          <label htmlFor="label-name" className="block text-xs font-medium mb-1" style={{ color: 'var(--card-description-color, #605e5c)' }}>
            Name
          </label>
          <input
            id="label-name"
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="e.g. Bug, Feature"
            maxLength={100}
            className="w-full px-3 py-2 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          />
        </div>
        <div className="flex items-center gap-2">
          <label className="block text-xs font-medium mb-1" style={{ color: 'var(--card-description-color, #605e5c)' }}>
            Color
          </label>
          <div className="flex gap-1.5">
            {PRESET_COLORS.map((c) => (
              <button
                key={c}
                type="button"
                onClick={() => setColor(c)}
                className={`w-8 h-8 rounded-lg border-2 transition-all ${color === c ? 'border-gray-900 dark:border-white scale-110' : 'border-transparent hover:scale-105'}`}
                style={{ backgroundColor: c }}
                aria-label={`Color ${c}`}
              />
            ))}
          </div>
        </div>
        <button
          type="submit"
          disabled={saving || !name.trim()}
          className="px-4 py-2 rounded-xl bg-blue-600 text-white text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
        >
          {saving ? 'Adding…' : 'Add label'}
        </button>
      </form>

      <div className="rounded-xl border border-gray-100 dark:border-slate-700/50 overflow-hidden bg-white dark:bg-slate-800/50 shadow-sm">
        <ul className="divide-y divide-gray-100 dark:divide-slate-700/50">
          {labels.length === 0 ? (
            <li className="px-4 py-8 text-center text-sm" style={{ color: 'var(--card-description-color, #605e5c)' }}>
              No labels yet. Add labels to tag tasks.
            </li>
          ) : (
            labels.map((label) => (
              <li key={label.id} className="flex items-center justify-between gap-4 px-4 py-3 hover:bg-gray-50 dark:hover:bg-slate-700/30 transition-colors">
                <span
                  className="inline-flex items-center gap-2 px-2.5 py-1 rounded-lg text-sm font-medium"
                  style={{ backgroundColor: label.color, color: label.color === '#ffffff' || label.color === '#fff' ? '#333' : '#fff' }}
                >
                  {label.name}
                </span>
                <button
                  type="button"
                  onClick={() => handleDelete(label.id)}
                  disabled={deletingId === label.id}
                  className="text-sm text-red-600 dark:text-red-400 hover:underline disabled:opacity-50"
                >
                  {deletingId === label.id ? 'Removing…' : 'Remove'}
                </button>
              </li>
            ))
          )}
        </ul>
      </div>
    </div>
  );
}
