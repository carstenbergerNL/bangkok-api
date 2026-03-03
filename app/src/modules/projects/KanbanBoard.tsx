import { useCallback } from 'react';
import {
  DndContext,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core';
import { KanbanColumn } from './KanbanColumn';
import type { Task } from './types';

const COLUMN_IDS = ['ToDo', 'InProgress', 'Done'] as const;

interface KanbanBoardProps {
  tasks: Task[];
  setTasks: React.Dispatch<React.SetStateAction<Task[]>>;
  userMap: Map<string, string>;
  canDrag: boolean;
  onTaskClick: (task: Task) => void;
  onMoveTask: (taskId: string, newStatus: string) => Promise<void>;
  onTaskDelete?: (task: Task) => void;
  canDelete?: boolean;
}

export function KanbanBoard({
  tasks,
  setTasks,
  userMap,
  canDrag,
  onTaskClick,
  onMoveTask,
  onTaskDelete,
  canDelete,
}: KanbanBoardProps) {
  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: { distance: 8 },
    })
  );

  const handleDragEnd = useCallback(
    async (event: DragEndEvent) => {
      const { active, over } = event;
      if (!over || active.id === over.id) return;
      const taskId = String(active.id);
      const overId = String(over.id);
      if (!COLUMN_IDS.includes(overId as (typeof COLUMN_IDS)[number])) return;
      const task = tasks.find((t) => t.id === taskId);
      if (!task || (task.status || 'ToDo') === overId) return;

      const previousStatus = task.status || 'ToDo';
      setTasks((prev) => prev.map((t) => (t.id === taskId ? { ...t, status: overId } : t)));

      try {
        await onMoveTask(taskId, overId);
      } catch {
        setTasks((prev) => prev.map((t) => (t.id === taskId ? { ...t, status: previousStatus } : t)));
      }
    },
    [tasks, setTasks, onMoveTask]
  );

  const byStatus = (status: string) => tasks.filter((t) => (t.status || 'ToDo') === status);

  return (
    <DndContext sensors={sensors} onDragEnd={handleDragEnd}>
      <div className="flex gap-4 overflow-x-auto overflow-y-hidden pb-4 min-h-[400px] min-w-0">
        {COLUMN_IDS.map((status) => (
          <KanbanColumn
            key={status}
            status={status}
            tasks={byStatus(status)}
            userMap={userMap}
            canDrag={canDrag}
            onTaskClick={onTaskClick}
            onTaskDelete={onTaskDelete}
            canDelete={canDelete}
          />
        ))}
      </div>
    </DndContext>
  );
}
