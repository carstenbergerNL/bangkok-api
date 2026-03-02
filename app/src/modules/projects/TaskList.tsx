import { useCallback, useEffect, useState } from 'react';
import { usePermissions } from '../../hooks/usePermissions';
import { PERMISSIONS } from '../../constants/permissions';
import { getTasks, deleteTask, createTask, updateTask } from './taskService';
import { addToast } from '../../utils/toast';
import { Modal } from '../../components/Modal';
import type { Task } from './types';
import { TaskFormModal } from './TaskFormModal';

const SECTIONS: { key: string; label: string }[] = [
  { key: 'ToDo', label: 'Todo' },
  { key: 'InProgress', label: 'In Progress' },
  { key: 'Done', label: 'Done' },
];

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

interface TaskListProps {
  projectId: string;
  userMap: Map<string, string>;
}

export function TaskList({ projectId, userMap }: TaskListProps) {
  const { hasPermission } = usePermissions();
  const [tasks, setTasks] = useState<Task[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingTask, setEditingTask] = useState<Task | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<Task | null>(null);
  const [deleting, setDeleting] = useState(false);

  const loadTasks = useCallback(() => {
    setLoading(true);
    getTasks(projectId)
      .then((res) => {
        const data = res.data ?? (res as unknown as { Data?: Task[] }).Data;
        if (res.success && Array.isArray(data)) {
          setTasks(data);
        }
      })
      .finally(() => setLoading(false));
  }, [projectId]);

  useEffect(() => {
    loadTasks();
  }, [loadTasks]);

  const handleAddTask = () => {
    setEditingTask(null);
    setModalOpen(true);
  };

  const handleEditTask = (t: Task) => {
    setEditingTask(t);
    setModalOpen(true);
  };

  const handleModalSaved = () => {
    loadTasks();
  };

  const handleCloseModal = () => {
    setModalOpen(false);
    setEditingTask(null);
  };

  const handleDeleteClick = (t: Task) => setDeleteConfirm(t);
  const handleDeleteCancel = () => setDeleteConfirm(null);

  const handleDeleteConfirm = async () => {
    if (!deleteConfirm) return;
    setDeleting(true);
    try {
      const res = await deleteTask(deleteConfirm.id);
      if (res.success) {
        addToast('success', 'Task deleted.');
        setDeleteConfirm(null);
        loadTasks();
      } else {
        addToast('error', res.error?.message ?? 'Failed to delete task.');
      }
    } finally {
      setDeleting(false);
    }
  };

  const canCreate = hasPermission(PERMISSIONS.TaskCreate);
  const canEdit = hasPermission(PERMISSIONS.TaskEdit);
  const canDelete = hasPermission(PERMISSIONS.TaskDelete);
  const canAssign = hasPermission(PERMISSIONS.TaskAssign);

  const [draggedTaskId, setDraggedTaskId] = useState<string | null>(null);
  const [dropTargetStatus, setDropTargetStatus] = useState<string | null>(null);

  const byStatus = (status: string) => tasks.filter((t) => (t.status || 'ToDo') === status);

  const handleDragStart = useCallback(
    (e: React.DragEvent, task: Task) => {
      if (!canEdit) return;
      setDraggedTaskId(task.id);
      e.dataTransfer.setData('application/json', JSON.stringify({ taskId: task.id }));
      e.dataTransfer.effectAllowed = 'move';
      const card = (e.currentTarget as HTMLElement).closest('[data-task-card]') as HTMLElement | null;
      if (card) e.dataTransfer.setDragImage(card, 0, 0);
    },
    [canEdit]
  );

  const handleDragEnd = useCallback(() => {
    setDraggedTaskId(null);
    setDropTargetStatus(null);
  }, []);

  const handleDragOver = useCallback((e: React.DragEvent, status: string) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    setDropTargetStatus(status);
  }, []);

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    const related = e.relatedTarget as Node | null;
    if (!e.currentTarget.contains(related)) setDropTargetStatus(null);
  }, []);

  const handleDrop = useCallback(
    async (e: React.DragEvent, targetStatus: string) => {
      e.preventDefault();
      setDropTargetStatus(null);
      setDraggedTaskId(null);
      try {
        const raw = e.dataTransfer.getData('application/json');
        if (!raw) return;
        const { taskId } = JSON.parse(raw) as { taskId: string };
        const task = tasks.find((t) => t.id === taskId);
        if (!task || (task.status || 'ToDo') === targetStatus) return;
        const res = await updateTask(taskId, { status: targetStatus });
        if (res.success) {
          setTasks((prev) => prev.map((t) => (t.id === taskId ? { ...t, status: targetStatus } : t)));
          addToast('success', 'Task updated.');
        } else {
          addToast('error', res.error?.message ?? 'Failed to update task.');
          loadTasks();
        }
      } catch {
        addToast('error', 'Failed to update task.');
        loadTasks();
      }
    },
    [tasks, loadTasks]
  );

  if (loading) {
    return (
      <div className="space-y-4">
        <div className="grid gap-4 md:grid-cols-3">
          {[1, 2, 3].map((i) => (
            <div key={i} className="rounded-xl border border-gray-100 dark:border-slate-700/50 bg-white dark:bg-slate-800/50 p-4 animate-pulse">
              <div className="h-5 w-1/2 bg-gray-200 dark:bg-slate-600 rounded mb-3" />
              <div className="space-y-2">
                <div className="h-16 bg-gray-100 dark:bg-slate-700 rounded" />
                <div className="h-16 bg-gray-100 dark:bg-slate-700 rounded" />
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <h2 className="text-lg font-semibold" style={{ color: 'var(--card-header-color, #323130)' }}>Tasks</h2>
        {canCreate && (
          <button
            type="button"
            onClick={handleAddTask}
            className="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-blue-600 text-white text-sm font-medium shadow-sm hover:bg-blue-700 transition-colors"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            Add task
          </button>
        )}
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        {SECTIONS.map(({ key, label }) => (
          <div key={key} className="rounded-xl border border-gray-100 dark:border-slate-700/50 bg-white dark:bg-slate-800/50 shadow-sm overflow-hidden">
            <div className="px-4 py-3 border-b border-gray-100 dark:border-slate-700" style={{ backgroundColor: 'var(--sidebar-bg, #faf9f8)' }}>
              <h3 className="text-sm font-semibold" style={{ color: 'var(--card-header-color, #323130)' }}>{label}</h3>
              <p className="text-xs mt-0.5" style={{ color: 'var(--card-description-color, #605e5c)' }}>{byStatus(key).length} task(s)</p>
            </div>
            <div
              className={`p-4 space-y-3 min-h-[120px] transition-colors duration-150 ${dropTargetStatus === key ? 'ring-2 ring-blue-500 ring-inset bg-blue-50/50 dark:bg-blue-900/10' : ''}`}
              onDragOver={canEdit ? (e) => handleDragOver(e, key) : undefined}
              onDragLeave={canEdit ? handleDragLeave : undefined}
              onDrop={canEdit ? (e) => handleDrop(e, key) : undefined}
            >
              {byStatus(key).length === 0 ? (
                <p className="text-sm" style={{ color: 'var(--card-description-color, #605e5c)' }}>No tasks</p>
              ) : (
                byStatus(key).map((t) => (
                  <div
                    key={t.id}
                    data-task-card
                    className={`rounded-lg border border-gray-100 dark:border-slate-700 p-3 bg-white dark:bg-slate-800 shadow-sm hover:shadow transition-shadow ${draggedTaskId === t.id ? 'opacity-50' : ''}`}
                  >
                    <div className="flex items-start justify-between gap-2">
                      <div className="flex items-start gap-2 min-w-0 flex-1">
                        {canEdit && (
                          <span
                            draggable
                            onDragStart={(e) => handleDragStart(e, t)}
                            onDragEnd={handleDragEnd}
                            className="cursor-grab active:cursor-grabbing touch-none p-0.5 rounded text-gray-400 hover:text-gray-600 dark:hover:text-slate-500 dark:hover:text-slate-300 shrink-0"
                            aria-label="Drag to move"
                          >
                            <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
                              <path d="M8 6h2v2H8V6zm0 5h2v2H8v-2zm0 5h2v2H8v-2zm5-10h2v2h-2V6zm0 5h2v2h-2v-2zm0 5h2v2h-2v-2z" />
                            </svg>
                          </span>
                        )}
                        <h4 className="font-medium text-sm truncate" style={{ color: 'var(--card-header-color, #323130)' }}>{t.title}</h4>
                      </div>
                      <div className="flex items-center gap-1 shrink-0">
                        {canEdit && (
                          <button
                            type="button"
                            onClick={() => handleEditTask(t)}
                            className="p-1 rounded hover:bg-gray-100 dark:hover:bg-slate-700 text-gray-500 dark:text-slate-400"
                            aria-label="Edit task"
                          >
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                            </svg>
                          </button>
                        )}
                        {canDelete && (
                          <button
                            type="button"
                            onClick={() => handleDeleteClick(t)}
                            className="p-1 rounded hover:bg-red-50 dark:hover:bg-red-900/20 text-gray-500 dark:text-slate-400 hover:text-red-600"
                            aria-label="Delete task"
                          >
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                            </svg>
                          </button>
                        )}
                      </div>
                    </div>
                    <div className="mt-2 flex flex-wrap gap-1.5 items-center">
                      <span className={`inline-flex px-2 py-0.5 rounded text-xs font-medium ${getPriorityClass(t.priority)}`}>
                        {t.priority}
                      </span>
                      <span className="text-xs" style={{ color: 'var(--card-description-color, #605e5c)' }}>
                        {t.assignedToUserId ? userMap.get(t.assignedToUserId) ?? '—' : 'Unassigned'}
                      </span>
                      {t.dueDate && (
                        <span className="text-xs" style={{ color: 'var(--card-description-color, #605e5c)' }}>
                          Due {formatDate(t.dueDate)}
                        </span>
                      )}
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        ))}
      </div>

      <TaskFormModal
        open={modalOpen}
        onClose={handleCloseModal}
        onSaved={handleModalSaved}
        projectId={projectId}
        task={editingTask}
        save={updateTask}
        create={createTask}
        canAssign={canAssign}
      />

      {deleteConfirm && (
        <Modal open={!!deleteConfirm} onClose={handleDeleteCancel} title="Delete task">
          <p className="text-sm mb-4" style={{ color: 'var(--card-description-color, #605e5c)' }}>
            Delete &quot;{deleteConfirm.title}&quot;? This cannot be undone.
          </p>
          <div className="flex justify-end gap-2">
            <button type="button" onClick={handleDeleteCancel} className="px-4 py-2 rounded-lg border border-gray-200 dark:border-slate-600 hover:bg-gray-50 dark:hover:bg-slate-700 transition-colors">
              Cancel
            </button>
            <button type="button" onClick={handleDeleteConfirm} disabled={deleting} className="px-4 py-2 rounded-lg bg-red-600 text-white hover:bg-red-700 disabled:opacity-50 transition-colors">
              {deleting ? 'Deleting…' : 'Delete'}
            </button>
          </div>
        </Modal>
      )}
    </div>
  );
}
