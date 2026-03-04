import { useCallback, useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { getLabels, createLabel, deleteLabel } from './labelService';
import { addToast } from '../../utils/toast';
import type { Label, CreateLabelRequest } from './types';

/** Extended palette: red, orange, amber, yellow, lime, green, emerald, teal, cyan, sky, blue, indigo, violet, purple, fuchsia, pink, rose, gray, plus neutrals */
const PRESET_COLORS = [
  '#dc2626', '#ef4444', '#f87171', '#f97316', '#fb923c', '#f59e0b', '#fbbf24', '#eab308',
  '#a3e635', '#84cc16', '#22c55e', '#34d399', '#10b981', '#14b8a6', '#2dd4bf', '#06b6d4',
  '#0ea5e9', '#38bdf8', '#3b82f6', '#60a5fa', '#6366f1', '#818cf8', '#8b5cf6', '#a78bfa',
  '#a855f7', '#c084fc', '#d946ef', '#e879f9', '#ec4899', '#f472b6', '#f43f5e', '#fb7185',
  '#e11d48', '#64748b', '#94a3b8', '#6b7280', '#9ca3af', '#4b5563', '#6b7280',
  '#374151', '#1f2937', '#111827', '#ffffff',
];
const DEFAULT_COLOR = PRESET_COLORS[0];

interface ProjectLabelsSettingsProps {
  projectId: string;
}

export function ProjectLabelsSettings({ projectId }: ProjectLabelsSettingsProps) {
  const [labels, setLabels] = useState<Label[]>([]);
  const [loading, setLoading] = useState(true);
  const [name, setName] = useState('');
  const [color, setColor] = useState(DEFAULT_COLOR);
const [colorPopupOpen, setColorPopupOpen] = useState(false);
const [popupPosition, setPopupPosition] = useState<{ top: number; left: number } | null>(null);
const colorPopupRef = useRef<HTMLDivElement>(null);
const triggerRef = useRef<HTMLButtonElement>(null);
const popupContentRef = useRef<HTMLDivElement>(null);
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

  useEffect(() => {
    if (!colorPopupOpen) return;
    const rect = triggerRef.current?.getBoundingClientRect();
    if (rect) setPopupPosition({ top: rect.bottom + 8, left: rect.left });
  }, [colorPopupOpen]);

  useEffect(() => {
    if (!colorPopupOpen) return;
    const close = (e: MouseEvent) => {
      const target = e.target as Node;
      if (
        colorPopupRef.current?.contains(target) ||
        popupContentRef.current?.contains(target)
      ) return;
      setColorPopupOpen(false);
    };
    document.addEventListener('mousedown', close);
    return () => document.removeEventListener('mousedown', close);
  }, [colorPopupOpen]);

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
        setColor(DEFAULT_COLOR);
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
        <div className="relative" ref={colorPopupRef}>
          <label className="block text-xs font-medium mb-1" style={{ color: 'var(--card-description-color, #605e5c)' }}>
            Color
          </label>
          <button
            ref={triggerRef}
            type="button"
            onClick={() => setColorPopupOpen((o) => !o)}
            className="flex items-center gap-2 px-3 py-2 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm hover:bg-gray-50 dark:hover:bg-slate-700 transition-colors"
            aria-label="Choose color"
            aria-expanded={colorPopupOpen}
            aria-haspopup="true"
          >
            <span
              className="w-6 h-6 rounded border border-gray-200 dark:border-slate-500 shrink-0"
              style={{ backgroundColor: color }}
            />
            <span style={{ color: 'var(--card-description-color, #605e5c)' }}>Pick color</span>
          </button>
          {colorPopupOpen && popupPosition && createPortal(
            <div
              ref={popupContentRef}
              className="fixed z-[9999] w-max max-w-[min(100vw-2rem,400px)] p-4 rounded-xl border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 shadow-lg"
              role="dialog"
              aria-label="Label color palette"
              style={{ top: popupPosition.top, left: popupPosition.left }}
            >
              <p className="text-xs font-medium text-gray-500 dark:text-slate-400 mb-3">Choose color</p>
              <div className="grid grid-cols-8 gap-2">
                {PRESET_COLORS.map((c) => (
                  <button
                    key={c}
                    type="button"
                    onClick={() => {
                      setColor(c);
                      setColorPopupOpen(false);
                    }}
                    className={`size-8 rounded-lg border transition-colors shrink-0 ${color === c ? 'ring-2 ring-offset-1 ring-blue-500 border-white dark:border-slate-200 shadow-sm' : 'border-gray-300/80 dark:border-slate-500 hover:border-gray-400 dark:hover:border-slate-400'}`}
                    style={{ backgroundColor: c }}
                    aria-label={`Color ${c}`}
                  />
                ))}
              </div>
            </div>,
            document.body
          )}
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
