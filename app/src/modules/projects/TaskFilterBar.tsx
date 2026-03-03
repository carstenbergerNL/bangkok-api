import { useState, useEffect, useRef } from 'react';
import type { TaskFilterParams, Label } from './types';
import { TASK_STATUSES, TASK_PRIORITIES } from './types';

interface TaskFilterBarProps {
  filter: TaskFilterParams;
  onChange: (filter: TaskFilterParams) => void;
  onClear: () => void;
  users: { id: string; displayName: string }[];
  labels: Label[];
}

const STATUS_LABELS: Record<string, string> = { ToDo: 'Todo', InProgress: 'In Progress', Done: 'Done' };

export function TaskFilterBar({ filter, onChange, users, labels, onClear }: TaskFilterBarProps) {
  const [searchInput, setSearchInput] = useState(filter.search ?? '');
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    setSearchInput(filter.search ?? '');
  }, [filter.search]);

  useEffect(() => {
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
  }, []);

  const hasActive =
    !!filter.status ||
    !!filter.priority ||
    !!filter.assignedToUserId ||
    !!filter.labelId ||
    !!filter.dueBefore ||
    !!filter.dueAfter ||
    !!(filter.search?.trim());

  const update = (key: keyof TaskFilterParams, value: string | undefined) => {
    const next = { ...filter, [key]: value };
    if (!value) delete next[key];
    onChange(next);
  };

  const handleSearchChange = (value: string) => {
    setSearchInput(value);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      update('search', value.trim() || undefined);
      debounceRef.current = null;
    }, 400);
  };

  return (
    <div className="flex flex-wrap items-center gap-3 py-3 px-4 rounded-xl border border-gray-100 dark:border-slate-700/50 bg-gray-50/50 dark:bg-slate-800/30">
      <input
        type="text"
        placeholder="Search tasks..."
        value={searchInput}
        onChange={(e) => handleSearchChange(e.target.value)}
        className="w-40 min-w-0 px-3 py-1.5 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm placeholder:text-gray-400 focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
        aria-label="Search tasks"
      />
      <select
        value={filter.status ?? ''}
        onChange={(e) => update('status', e.target.value || undefined)}
        className="px-3 py-1.5 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
        aria-label="Filter by status"
      >
        <option value="">All statuses</option>
        {TASK_STATUSES.map((s) => (
          <option key={s} value={s}>{STATUS_LABELS[s] ?? s}</option>
        ))}
      </select>
      <select
        value={filter.priority ?? ''}
        onChange={(e) => update('priority', e.target.value || undefined)}
        className="px-3 py-1.5 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
        aria-label="Filter by priority"
      >
        <option value="">All priorities</option>
        {TASK_PRIORITIES.map((p) => (
          <option key={p} value={p}>{p}</option>
        ))}
      </select>
      <select
        value={filter.assignedToUserId ?? ''}
        onChange={(e) => update('assignedToUserId', e.target.value || undefined)}
        className="px-3 py-1.5 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
        aria-label="Filter by assignee"
      >
        <option value="">All assignees</option>
        {users.map((u) => (
          <option key={u.id} value={u.id}>{u.displayName || u.id}</option>
        ))}
      </select>
      {labels.length > 0 && (
        <select
          value={filter.labelId ?? ''}
          onChange={(e) => update('labelId', e.target.value || undefined)}
          className="px-3 py-1.5 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          aria-label="Filter by label"
        >
          <option value="">All labels</option>
          {labels.map((l) => (
            <option key={l.id} value={l.id}>{l.name}</option>
          ))}
        </select>
      )}
      <div className="flex items-center gap-2">
        <input
          type="date"
          value={filter.dueAfter ?? ''}
          onChange={(e) => update('dueAfter', e.target.value || undefined)}
          className="px-3 py-1.5 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          aria-label="Due after"
          title="Due after"
        />
        <span className="text-gray-400 dark:text-slate-500 text-sm">–</span>
        <input
          type="date"
          value={filter.dueBefore ?? ''}
          onChange={(e) => update('dueBefore', e.target.value || undefined)}
          className="px-3 py-1.5 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          aria-label="Due before"
          title="Due before"
        />
      </div>
      {hasActive && (
        <button
          type="button"
          onClick={onClear}
          className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg text-sm font-medium text-gray-600 dark:text-slate-400 hover:bg-gray-200 dark:hover:bg-slate-600 hover:text-gray-900 dark:hover:text-slate-200 transition-colors"
        >
          Clear filters
        </button>
      )}
    </div>
  );
}
