import { memo } from 'react';
import { useDroppable } from '@dnd-kit/core';
import type { Task } from './types';
import { DraggableTaskCard } from './DraggableTaskCard';

const COLUMN_TINTS: Record<string, string> = {
  ToDo: 'bg-gray-50/80 dark:bg-slate-800/50',
  InProgress: 'bg-blue-50/80 dark:bg-blue-900/10',
  Done: 'bg-emerald-50/80 dark:bg-emerald-900/10',
};

const COLUMN_LABELS: Record<string, string> = {
  ToDo: 'Todo',
  InProgress: 'In Progress',
  Done: 'Done',
};

interface KanbanColumnProps {
  status: string;
  tasks: Task[];
  userMap: Map<string, string>;
  canDrag: boolean;
  onTaskClick: (task: Task) => void;
  onTaskDelete?: (task: Task) => void;
  canDelete?: boolean;
}

function KanbanColumnInner({ status, tasks, userMap, canDrag, onTaskClick, onTaskDelete, canDelete }: KanbanColumnProps) {
  const { setNodeRef, isOver } = useDroppable({ id: status });
  const tint = COLUMN_TINTS[status] ?? COLUMN_TINTS.ToDo;
  const label = COLUMN_LABELS[status] ?? status;

  return (
    <div
      ref={setNodeRef}
      className={`flex flex-col rounded-xl border border-gray-100 dark:border-slate-700/50 shadow-sm min-w-[200px] flex-1 overflow-hidden ${tint} ${isOver ? 'ring-2 ring-blue-400 ring-inset' : ''}`}
    >
      <div className="sticky top-0 z-10 px-4 py-3 border-b border-gray-100 dark:border-slate-700 bg-white/90 dark:bg-slate-800/90 backdrop-blur-sm">
        <h3 className="text-sm font-semibold" style={{ color: 'var(--card-header-color, #323130)' }}>
          {label}
        </h3>
        <p className="text-xs mt-0.5" style={{ color: 'var(--card-description-color, #605e5c)' }}>
          {tasks.length} task{tasks.length !== 1 ? 's' : ''}
        </p>
      </div>
      <div className="flex-1 overflow-y-auto p-4 space-y-3 min-h-[160px] max-h-[calc(100vh-320px)]">
        {tasks.length === 0 ? (
          <p className="text-sm py-4" style={{ color: 'var(--card-description-color, #605e5c)' }}>
            No tasks
          </p>
        ) : (
          tasks.map((t) => (
            <DraggableTaskCard
              key={t.id}
              task={t}
              userMap={userMap}
              canDrag={canDrag}
              onClick={() => onTaskClick(t)}
              onDelete={onTaskDelete}
              canDelete={canDelete}
            />
          ))
        )}
      </div>
    </div>
  );
}

export const KanbanColumn = memo(KanbanColumnInner);
