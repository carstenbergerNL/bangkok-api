import { useCallback, useEffect, useState, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { usePermissions } from '../../hooks/usePermissions';
import { PERMISSIONS } from '../../constants/permissions';
import { getTasks, deleteTask, createTask, updateTask } from './taskService';
import { getLabels } from './labelService';
import { addToast } from '../../utils/toast';
import { Modal } from '../../components/Modal';
import type { Task, TaskFilterParams, Label } from './types';
import { KanbanBoard } from './KanbanBoard';
import { TaskDrawer } from './TaskDrawer';
import { TaskFormModal } from './TaskFormModal';
import { TaskFilterBar } from './TaskFilterBar';

const COLUMN_LABELS: Record<string, string> = {
  ToDo: 'Todo',
  InProgress: 'In Progress',
  Done: 'Done',
};

interface TaskListProps {
  projectId: string;
  userMap: Map<string, string>;
}

export function TaskList({ projectId, userMap }: TaskListProps) {
  const { hasPermission } = usePermissions();
  const [searchParams, setSearchParams] = useSearchParams();
  const [tasks, setTasks] = useState<Task[]>([]);
  const [loading, setLoading] = useState(true);
  const [labels, setLabels] = useState<Label[]>([]);
  const [addModalOpen, setAddModalOpen] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [selectedTask, setSelectedTask] = useState<Task | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<Task | null>(null);
  const [deleting, setDeleting] = useState(false);

  const filter = useMemo<TaskFilterParams>(() => ({
    ...(searchParams.get('status') && { status: searchParams.get('status')! }),
    ...(searchParams.get('priority') && { priority: searchParams.get('priority')! }),
    ...(searchParams.get('assignedToUserId') && { assignedToUserId: searchParams.get('assignedToUserId')! }),
    ...(searchParams.get('labelId') && { labelId: searchParams.get('labelId')! }),
    ...(searchParams.get('dueBefore') && { dueBefore: searchParams.get('dueBefore')! }),
    ...(searchParams.get('dueAfter') && { dueAfter: searchParams.get('dueAfter')! }),
    ...(searchParams.get('search') && { search: searchParams.get('search')! }),
  }), [searchParams]);

  const loadTasks = useCallback(() => {
    setLoading(true);
    const hasFilter = Object.keys(filter).length > 0;
    getTasks(projectId, hasFilter ? filter : undefined)
      .then((res) => {
        const data = res.data ?? (res as unknown as { Data?: Task[] }).Data;
        if (res.success && Array.isArray(data)) {
          setTasks(data);
        }
      })
      .finally(() => setLoading(false));
  }, [projectId, filter]);

  useEffect(() => {
    getLabels(projectId).then((res) => {
      const data = res.data ?? (res as unknown as { Data?: Label[] }).Data;
      setLabels(Array.isArray(data) ? data : []);
    });
  }, [projectId]);

  useEffect(() => {
    loadTasks();
  }, [loadTasks]);

  const handleFilterChange = useCallback((newFilter: TaskFilterParams) => {
    const next = new URLSearchParams();
    if (newFilter.status) next.set('status', newFilter.status);
    if (newFilter.priority) next.set('priority', newFilter.priority);
    if (newFilter.assignedToUserId) next.set('assignedToUserId', newFilter.assignedToUserId);
    if (newFilter.labelId) next.set('labelId', newFilter.labelId);
    if (newFilter.dueBefore) next.set('dueBefore', newFilter.dueBefore);
    if (newFilter.dueAfter) next.set('dueAfter', newFilter.dueAfter);
    if (newFilter.search?.trim()) next.set('search', newFilter.search.trim());
    setSearchParams(next, { replace: true });
  }, [setSearchParams]);

  const handleClearFilters = useCallback(() => {
    setSearchParams({}, { replace: true });
  }, [setSearchParams]);

  const usersForFilter = useMemo(() => {
    const list: { id: string; displayName: string }[] = [];
    userMap.forEach((displayName, id) => list.push({ id, displayName }));
    return list;
  }, [userMap]);

  const handleAddTask = () => {
    setAddModalOpen(true);
  };

  const handleTaskClick = useCallback((task: Task) => {
    setSelectedTask(task);
    setDrawerOpen(true);
  }, []);

  const handleDrawerClose = useCallback(() => {
    setDrawerOpen(false);
    setSelectedTask(null);
  }, []);

  const handleDrawerSaved = useCallback(() => {
    loadTasks();
  }, [loadTasks]);

  const handleMoveTask = useCallback(
    async (taskId: string, newStatus: string) => {
      const res = await updateTask(taskId, { status: newStatus });
      if (!res.success) {
        addToast('error', res.error?.message ?? 'Failed to move task.');
        throw new Error('Move failed');
      }
      const label = COLUMN_LABELS[newStatus] ?? newStatus;
      addToast('success', `Task moved to ${label}.`);
      if (typeof console !== 'undefined' && console.log) {
        console.log('[Activity] Task moved', { taskId, newStatus, label });
      }
    },
    []
  );

  const handleDeleteClick = useCallback((task: Task) => {
    setDrawerOpen(false);
    setSelectedTask(null);
    setDeleteConfirm(task);
  }, []);

  const handleDeleteCancel = useCallback(() => setDeleteConfirm(null), []);

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
  const canComment = hasPermission(PERMISSIONS.TaskComment);
  const canViewActivity = hasPermission(PERMISSIONS.TaskViewActivity);
  const canAssign = hasPermission(PERMISSIONS.TaskAssign);

  if (loading) {
    return (
      <div className="space-y-4">
        <div className="flex gap-4 overflow-x-auto pb-4">
          {[1, 2, 3].map((i) => (
            <div
              key={i}
              className="rounded-xl border border-gray-100 dark:border-slate-700/50 min-w-[280px] w-[280px] flex-shrink-0 p-4 animate-pulse bg-gray-50/50 dark:bg-slate-800/50"
            >
              <div className="h-5 w-24 bg-gray-200 dark:bg-slate-600 rounded mb-3" />
              <div className="space-y-3 mt-4">
                <div className="h-20 bg-gray-100 dark:bg-slate-700 rounded-xl" />
                <div className="h-20 bg-gray-100 dark:bg-slate-700 rounded-xl" />
                <div className="h-20 bg-gray-100 dark:bg-slate-700 rounded-xl" />
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <>
      <div className="min-w-0">
        <div className="space-y-6 min-w-0">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <h2 className="text-lg font-semibold" style={{ color: 'var(--card-header-color, #323130)' }}>
              Tasks
            </h2>
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

          <TaskFilterBar
            filter={filter}
            onChange={handleFilterChange}
            onClear={handleClearFilters}
            users={usersForFilter}
            labels={labels}
          />

          <KanbanBoard
            tasks={tasks}
            setTasks={setTasks}
            userMap={userMap}
            canDrag={canEdit}
            onTaskClick={handleTaskClick}
            onMoveTask={handleMoveTask}
            onTaskDelete={handleDeleteClick}
            canDelete={canDelete}
          />
        </div>
      </div>

      {drawerOpen && selectedTask && (
        <aside
          className="fixed top-12 right-0 z-40 h-[calc(100vh-3rem)] w-64 flex flex-col shrink-0 border-l transition-[width] duration-200"
          style={{
            backgroundColor: 'var(--sidebar-bg, #faf9f8)',
            borderColor: 'var(--sidebar-border, #edebe9)',
          }}
          role="complementary"
          aria-label="Task details"
        >
          <TaskDrawer
            open
            onClose={handleDrawerClose}
            onSaved={handleDrawerSaved}
            task={selectedTask}
            save={updateTask}
            canEdit={canEdit}
            canDelete={canDelete}
            canComment={canComment}
            canViewActivity={canViewActivity}
            onDelete={handleDeleteClick}
          />
        </aside>
      )}

      <TaskFormModal
        open={addModalOpen}
        onClose={() => setAddModalOpen(false)}
        onSaved={loadTasks}
        projectId={projectId}
        task={null}
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
            <button
              type="button"
              onClick={handleDeleteCancel}
              className="px-4 py-2 rounded-lg border border-gray-200 dark:border-slate-600 hover:bg-gray-50 dark:hover:bg-slate-700 transition-colors"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={handleDeleteConfirm}
              disabled={deleting}
              className="px-4 py-2 rounded-lg bg-red-600 text-white hover:bg-red-700 disabled:opacity-50 transition-colors"
            >
              {deleting ? 'Deleting…' : 'Delete'}
            </button>
          </div>
        </Modal>
      )}
    </>
  );
}
