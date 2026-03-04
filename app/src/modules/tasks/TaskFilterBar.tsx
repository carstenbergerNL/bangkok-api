import type { TasksStandaloneFilter } from './types';
import type { TenantAdminUser } from '../../services/tenantAdminService';

interface TaskFilterBarProps {
  filter: TasksStandaloneFilter;
  onChange: (f: TasksStandaloneFilter) => void;
  showAssignee: boolean;
  assigneeOptions: { id: string; label: string }[];
}

const STATUS_OPTIONS = [
  { value: '', label: 'All statuses' },
  { value: 'Open', label: 'Open' },
  { value: 'Completed', label: 'Completed' },
];

const PRIORITY_OPTIONS = [
  { value: '', label: 'All priorities' },
  { value: 'Low', label: 'Low' },
  { value: 'Medium', label: 'Medium' },
  { value: 'High', label: 'High' },
];

export function TaskFilterBar({ filter, onChange, showAssignee, assigneeOptions }: TaskFilterBarProps) {
  return (
    <div className="flex flex-wrap items-center gap-3 p-3 rounded-xl border border-gray-100 dark:border-slate-700/50 bg-gray-50/50 dark:bg-slate-800/30">
      <select
        value={filter.status ?? ''}
        onChange={(e) => onChange({ ...filter, status: e.target.value || undefined })}
        className="px-3 py-2 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm"
      >
        {STATUS_OPTIONS.map((o) => (
          <option key={o.value || 'all'} value={o.value}>{o.label}</option>
        ))}
      </select>
      <select
        value={filter.priority ?? ''}
        onChange={(e) => onChange({ ...filter, priority: e.target.value || undefined })}
        className="px-3 py-2 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm"
      >
        {PRIORITY_OPTIONS.map((o) => (
          <option key={o.value || 'all'} value={o.value}>{o.label}</option>
        ))}
      </select>
      {showAssignee && (
        <select
          value={filter.assignedToUserId ?? ''}
          onChange={(e) => onChange({ ...filter, assignedToUserId: e.target.value || undefined })}
          className="px-3 py-2 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm min-w-[140px]"
        >
          <option value="">All assignees</option>
          {assigneeOptions.map((u) => (
            <option key={u.id} value={u.id}>{u.label}</option>
          ))}
        </select>
      )}
      <input
        type="text"
        placeholder="Search..."
        value={filter.search ?? ''}
        onChange={(e) => onChange({ ...filter, search: e.target.value || undefined })}
        className="px-3 py-2 rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm w-48"
      />
    </div>
  );
}
