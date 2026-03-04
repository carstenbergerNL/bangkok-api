import { memo } from 'react';
import { useDraggable } from '@dnd-kit/core';
import type { Task } from './types';

function getPriorityClass(priority: string): string {
  const p = priority?.toLowerCase();
  if (p === 'high') return 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300';
  if (p === 'medium') return 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300';
  return 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300';
}

function formatDate(iso: string | null | undefined): string {
  if (!iso) return '—';
  try {
    return new Date(iso).toLocaleDateString(undefined, { dateStyle: 'medium' });
  } catch {
    return iso;
  }
}

function assigneeDisplay(userName: string | undefined): string {
  if (!userName) return 'Unassigned';
  return userName.length > 12 ? userName.slice(0, 10) + '…' : userName;
}

function avatarInitials(name: string | undefined): string {
  if (!name) return '?';
  const parts = name.trim().split(/\s+/);
  if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  return name.slice(0, 2).toUpperCase();
}

interface DraggableTaskCardProps {
  task: Task;
  userMap: Map<string, string>;
  canDrag: boolean;
  onClick: () => void;
  onDelete?: (task: Task) => void;
  canDelete?: boolean;
}

function DraggableTaskCardInner({ task, userMap, canDrag, onClick, onDelete, canDelete }: DraggableTaskCardProps) {
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({
    id: task.id,
    disabled: !canDrag,
  });

  const assigneeName = task.assignedToUserId ? userMap.get(task.assignedToUserId) ?? '—' : undefined;

  return (
    <div
      ref={setNodeRef}
      {...(canDrag ? { ...attributes, ...listeners } : {})}
      onClick={onClick}
      className={
        'relative rounded-xl border border-gray-100 dark:border-slate-700 p-3 bg-white dark:bg-slate-800 shadow-sm hover:shadow-md transition-shadow cursor-pointer ' +
        (canDrag ? 'cursor-grab active:cursor-grabbing touch-none ' : '') +
        (isDragging ? 'opacity-50 shadow-lg ring-2 ring-blue-400' : '')
      }
    >
      <div className="flex items-center gap-1.5 min-w-0">
        <h4 className="font-medium text-sm truncate flex-1 text-gray-900 dark:text-slate-100">
          {task.title}
        </h4>
        {task.isRecurring && (
          <span className="shrink-0 flex items-center" title="Recurring task">
            <svg className="w-4 h-4 text-indigo-500 dark:text-indigo-400" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden>
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
          </span>
        )}
      </div>
      {task.labels && task.labels.length > 0 && (
        <div className="mt-1.5 flex flex-wrap gap-1">
          {task.labels.slice(0, 4).map((label) => (
            <span
              key={label.id}
              className="inline-flex px-1.5 py-0.5 rounded text-[10px] font-medium truncate max-w-[72px]"
              style={{ backgroundColor: label.color, color: label.color === '#ffffff' || label.color === '#fff' ? '#333' : '#fff' }}
              title={label.name}
            >
              {label.name}
            </span>
          ))}
          {task.labels.length > 4 && (
            <span className="text-[10px] text-gray-500 dark:text-slate-400">+{task.labels.length - 4}</span>
          )}
        </div>
      )}
      <div className="mt-2 flex flex-wrap gap-1.5 items-center">
        <span className={'inline-flex px-2 py-0.5 rounded-lg text-xs font-medium ' + getPriorityClass(task.priority)}>
          {task.priority}
        </span>
        <span className="flex items-center gap-1.5 text-xs truncate text-gray-500 dark:text-slate-400">
          {assigneeName ? (
            <>
              <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-slate-200 dark:bg-slate-600 text-[10px] font-medium text-slate-600 dark:text-slate-300">
                {avatarInitials(assigneeName)}
              </span>
              {assigneeDisplay(assigneeName)}
            </>
          ) : (
            'Unassigned'
          )}
        </span>
        {task.dueDate && (
          <span className="text-xs text-gray-500 dark:text-slate-400">
            Due {formatDate(task.dueDate)}
          </span>
        )}
      </div>
      {canDelete && onDelete && (
        <button
          type="button"
          onClick={(e) => { e.stopPropagation(); onDelete(task); }}
          className="absolute top-2 right-2 p-1.5 rounded-lg hover:bg-red-50 dark:hover:bg-red-900/20 text-gray-400 hover:text-red-600 dark:text-slate-400 dark:hover:text-red-400"
          aria-label="Delete task"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
          </svg>
        </button>
      )}
    </div>
  );
}

export const DraggableTaskCard = memo(DraggableTaskCardInner);
