import { useCallback, useEffect, useState } from 'react';
import { getCustomFields, createCustomField, updateCustomField, deleteCustomField } from './customFieldService';
import { addToast } from '../../utils/toast';
import { Modal } from '../../components/Modal';
import type {
  ProjectCustomField,
  CreateProjectCustomFieldRequest,
  UpdateProjectCustomFieldRequest,
} from './types';

const FIELD_TYPES = ['Text', 'Number', 'Date', 'Dropdown'] as const;

interface ProjectCustomFieldsSettingsProps {
  projectId: string;
}

export function ProjectCustomFieldsSettings({ projectId }: ProjectCustomFieldsSettingsProps) {
  const [fields, setFields] = useState<ProjectCustomField[]>([]);
  const [loading, setLoading] = useState(true);
  const [name, setName] = useState('');
  const [fieldType, setFieldType] = useState<string>('Text');
  const [options, setOptions] = useState('');
  const [saving, setSaving] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [editingField, setEditingField] = useState<ProjectCustomField | null>(null);
  const [editName, setEditName] = useState('');
  const [editFieldType, setEditFieldType] = useState('Text');
  const [editOptions, setEditOptions] = useState('');
  const [savingEdit, setSavingEdit] = useState(false);

  const loadFields = useCallback(() => {
    setLoading(true);
    getCustomFields(projectId)
      .then((res) => {
        const data = res.data ?? (res as unknown as { Data?: ProjectCustomField[] }).Data;
        if (res.success && Array.isArray(data)) setFields(data);
        else addToast('error', res.error?.message ?? 'Failed to load custom fields.');
      })
      .finally(() => setLoading(false));
  }, [projectId]);

  useEffect(() => {
    loadFields();
  }, [loadFields]);

  const handleAdd = async (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = name.trim();
    if (!trimmed) return;
    setSaving(true);
    try {
      const request: CreateProjectCustomFieldRequest = {
        name: trimmed,
        fieldType: fieldType || 'Text',
        options: fieldType === 'Dropdown' && options.trim() ? options.trim() : undefined,
      };
      const res = await createCustomField(projectId, request);
      if (res.success && res.data) {
        setFields((prev) => [...prev, res.data!]);
        setName('');
        setFieldType('Text');
        setOptions('');
        addToast('success', 'Custom field added.');
      } else {
        addToast('error', res.error?.message ?? 'Failed to add field.');
      }
    } finally {
      setSaving(false);
    }
  };

  const handleEditOpen = (field: ProjectCustomField) => {
    setEditingField(field);
    setEditName(field.name);
    setEditFieldType(field.fieldType || 'Text');
    setEditOptions(field.options ?? '');
  };

  const handleEditSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingField || !editName.trim()) return;
    setSavingEdit(true);
    try {
      const request: UpdateProjectCustomFieldRequest = {
        name: editName.trim(),
        fieldType: editFieldType || 'Text',
        options: editFieldType === 'Dropdown' && editOptions.trim() ? editOptions.trim() : undefined,
      };
      const res = await updateCustomField(projectId, editingField.id, request);
      if (res.success && res.data) {
        setFields((prev) => prev.map((f) => (f.id === editingField.id ? res.data! : f)));
        setEditingField(null);
        addToast('success', 'Custom field updated.');
      } else {
        addToast('error', res.error?.message ?? 'Failed to update field.');
      }
    } finally {
      setSavingEdit(false);
    }
  };

  const handleDelete = async (fieldId: string) => {
    setDeletingId(fieldId);
    try {
      const res = await deleteCustomField(projectId, fieldId);
      if (res.success) {
        setFields((prev) => prev.filter((f) => f.id !== fieldId));
        addToast('success', 'Custom field removed.');
      } else {
        addToast('error', res.error?.message ?? 'Failed to remove field.');
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
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold mb-1" style={{ color: 'var(--card-header-color, #323130)' }}>
          Custom Fields
        </h2>
        <p className="text-sm mb-4" style={{ color: 'var(--card-description-color, #605e5c)' }}>
          Define fields that appear on tasks (e.g. Story Points, Due Date). Types: Text, Number, Date, Dropdown.
        </p>
      </div>

      <form onSubmit={handleAdd} className="space-y-3 p-4 rounded-xl border border-gray-100 dark:border-slate-700/50 bg-gray-50/50 dark:bg-slate-800/30">
        <div className="flex flex-wrap items-end gap-3">
          <div className="flex-1 min-w-[140px]">
            <label htmlFor="cf-name" className="block text-xs font-medium mb-1" style={{ color: 'var(--card-description-color, #605e5c)' }}>
              Name
            </label>
            <input
              id="cf-name"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g. Story Points"
              maxLength={100}
              className="input w-full text-sm"
            />
          </div>
          <div className="w-36">
            <label htmlFor="cf-type" className="block text-xs font-medium mb-1" style={{ color: 'var(--card-description-color, #605e5c)' }}>
              Type
            </label>
            <select
              id="cf-type"
              value={fieldType}
              onChange={(e) => setFieldType(e.target.value)}
              className="input w-full text-sm"
            >
              {FIELD_TYPES.map((t) => (
                <option key={t} value={t}>{t}</option>
              ))}
            </select>
          </div>
          {fieldType === 'Dropdown' && (
            <div className="flex-1 min-w-[180px]">
              <label htmlFor="cf-options" className="block text-xs font-medium mb-1" style={{ color: 'var(--card-description-color, #605e5c)' }}>
                Options (comma-separated)
              </label>
              <input
                id="cf-options"
                type="text"
                value={options}
                onChange={(e) => setOptions(e.target.value)}
                placeholder="e.g. Low, Medium, High"
                className="input w-full text-sm"
              />
            </div>
          )}
          <button
            type="submit"
            disabled={saving || !name.trim()}
            className="btn-primary text-sm"
          >
            {saving ? 'Adding…' : 'Add field'}
          </button>
        </div>
      </form>

      <div className="rounded-xl border border-gray-100 dark:border-slate-700/50 overflow-hidden bg-white dark:bg-slate-800/50 shadow-sm">
        <ul className="divide-y divide-gray-100 dark:divide-slate-700/50">
          {fields.length === 0 ? (
            <li className="px-4 py-8 text-center text-sm" style={{ color: 'var(--card-description-color, #605e5c)' }}>
              No custom fields. Add fields to show them on tasks.
            </li>
          ) : (
            fields.map((field) => (
              <li key={field.id} className="flex items-center justify-between gap-4 px-4 py-3 hover:bg-gray-50 dark:hover:bg-slate-700/30 transition-colors">
                <div className="min-w-0">
                  <span className="font-medium text-sm text-gray-900 dark:text-slate-100">{field.name}</span>
                  <span className="ml-2 text-xs text-gray-500 dark:text-slate-400">{field.fieldType}</span>
                  {field.fieldType === 'Dropdown' && field.options && (
                    <span className="ml-2 text-xs text-gray-400 dark:text-slate-500">({field.options})</span>
                  )}
                </div>
                <div className="flex items-center gap-2 shrink-0">
                  <button
                    type="button"
                    onClick={() => handleEditOpen(field)}
                    className="text-sm text-blue-600 dark:text-blue-400 hover:underline"
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    onClick={() => handleDelete(field.id)}
                    disabled={deletingId === field.id}
                    className="text-sm text-red-600 dark:text-red-400 hover:underline disabled:opacity-50"
                  >
                    {deletingId === field.id ? 'Removing…' : 'Remove'}
                  </button>
                </div>
              </li>
            ))
          )}
        </ul>
      </div>

      {editingField && (
        <Modal open title="Edit custom field" onClose={() => setEditingField(null)}>
          <form onSubmit={handleEditSave} className="space-y-4">
            <div>
              <label htmlFor="edit-cf-name" className="block text-sm font-medium mb-1" style={{ color: 'var(--card-header-color, #323130)' }}>
                Name
              </label>
              <input
                id="edit-cf-name"
                type="text"
                value={editName}
                onChange={(e) => setEditName(e.target.value)}
                className="input w-full"
                maxLength={100}
                required
              />
            </div>
            <div>
              <label htmlFor="edit-cf-type" className="block text-sm font-medium mb-1" style={{ color: 'var(--card-header-color, #323130)' }}>
                Type
              </label>
              <select
                id="edit-cf-type"
                value={editFieldType}
                onChange={(e) => setEditFieldType(e.target.value)}
                className="input w-full"
              >
                {FIELD_TYPES.map((t) => (
                  <option key={t} value={t}>{t}</option>
                ))}
              </select>
            </div>
            {editFieldType === 'Dropdown' && (
              <div>
                <label htmlFor="edit-cf-options" className="block text-sm font-medium mb-1" style={{ color: 'var(--card-header-color, #323130)' }}>
                  Options (comma-separated)
                </label>
                <input
                  id="edit-cf-options"
                  type="text"
                  value={editOptions}
                  onChange={(e) => setEditOptions(e.target.value)}
                  placeholder="e.g. Low, Medium, High"
                  className="input w-full"
                />
              </div>
            )}
            <div className="flex justify-end gap-2 pt-2">
              <button type="button" onClick={() => setEditingField(null)} className="btn-secondary">
                Cancel
              </button>
              <button type="submit" disabled={savingEdit || !editName.trim()} className="btn-primary">
                {savingEdit ? 'Saving…' : 'Save'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}
