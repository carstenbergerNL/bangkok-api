import type { TasksStandaloneTask } from './types';

function priorityBadgeClass(priority: string): string {
  const p = priority?.toLowerCase();
  if (p === 'high') return 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300';
  if (p === 'medium') return 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300';
  return 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300';
}

function formatDate(iso: string | null): string {
  if (!iso) return '';
  try {
    return new Date(iso).toLocaleDateString(undefined, { dateStyle: 'short' });
  } catch {
    return iso;
  }
}

function isOverdue(dueDate: string | null, status: string): boolean {
  if (!dueDate || status === 'Completed') return false;
  return new Date(dueDate) < new Date() && new Date(dueDate).toDateString() !== new Date().toDateString();
}

function initials(name: string | null): string {
  if (!name?.trim()) return '?';
  const parts = name.trim().split(/\s+/);
  if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  return name.slice(0, 2).toUpperCase();
}

interface TaskCardProps {
  task: TasksStandaloneTask;
  onSelect: () => void;
  onToggleComplete: (e: React.MouseEvent) => void;
  canEdit: boolean;
}

export function TaskCard({ task, onSelect, onToggleComplete, canEdit }: TaskCardProps) {
  const overdue = isOverdue(task.dueDate, task.status);
  const completed = task.status === 'Completed';

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={onSelect}
      onKeyDown={(e) => (e.key === 'Enter' || e.key === ' ') && onSelect()}
      className="flex items-start gap-3 p-4 rounded-xl border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 shadow-sm hover:shadow-md transition-shadow cursor-pointer text-left"
    >
      {canEdit && (
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation();
            onToggleComplete(e);
          }}
          className="mt-0.5 shrink-0 w-5 h-5 rounded border-2 border-gray-300 dark:border-slate-500 flex items-center justify-center focus:outline-none focus:ring-2 focus:ring-blue-500"
          aria-label={completed ? 'Reopen task' : 'Mark complete'}
        >
          {completed && (
            <svg className="w-3 h-3 text-green-600 dark:text-green-400" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
            </svg>
          )}
        </button>
      )}
      <div className="flex-1 min-w-0">
        <div className="flex flex-wrap items-center gap-2 mb-1">
          <span className={`font-medium ${completed ? 'line-through text-gray-500 dark:text-slate-400' : ''}`}>
            {task.title}
          </span>
          <span className={`text-xs px-2 py-0.5 rounded ${priorityBadgeClass(task.priority)}`}>
            {task.priority}
          </span>
        </div>
        {task.dueDate && (
          <span className={`text-xs ${overdue ? 'text-red-600 dark:text-red-400 font-medium' : 'text-gray-500 dark:text-slate-400'}`}>
            Due {formatDate(task.dueDate)}{overdue ? ' (overdue)' : ''}
          </span>
        )}
      </div>
      {task.assignedToDisplayName && (
        <span
          className="shrink-0 w-8 h-8 rounded-full bg-primary-100 dark:bg-primary-900/40 text-primary-700 dark:text-primary-300 flex items-center justify-center text-xs font-medium"
          title={task.assignedToDisplayName}
        >
          {initials(task.assignedToDisplayName)}
        </span>
      )}
    </div>
  );
}
