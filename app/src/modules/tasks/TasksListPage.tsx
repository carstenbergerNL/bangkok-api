import { useCallback, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { usePermissions } from '../../hooks/usePermissions';
import { PERMISSIONS } from '../../constants/permissions';
import { getTenantAdminUsers } from '../../services/tenantAdminService';
import {
  getTasksModuleList,
  getTasksModuleMy,
  setTasksModuleStatus,
} from './tasksService';
import { addToast } from '../../utils/toast';
import { TaskFilterBar } from './TaskFilterBar';
import { TaskCard } from './TaskCard';
import { TaskDrawer } from './TaskDrawer';
import type { TasksStandaloneTask, TasksStandaloneFilter } from './types';
import type { TenantAdminUser } from '../../services/tenantAdminService';

type Tab = 'my' | 'all';

export function TasksListPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const tab = (searchParams.get('tab') === 'all' ? 'all' : 'my') as Tab;
  const [tasks, setTasks] = useState<TasksStandaloneTask[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<TasksStandaloneFilter>({
    status: searchParams.get('status') ?? undefined,
    priority: searchParams.get('priority') ?? undefined,
    assignedToUserId: searchParams.get('assignedToUserId') ?? undefined,
    search: searchParams.get('search') ?? undefined,
  });
  const [drawerTaskId, setDrawerTaskId] = useState<string | null | 'new'>(null);
  const [assigneeOptions, setAssigneeOptions] = useState<TenantAdminUser[]>([]);

  const { hasPermission } = usePermissions();
  const canCreate = hasPermission(PERMISSIONS.TasksCreate);
  const canEdit = hasPermission(PERMISSIONS.TasksEdit);
  const canDelete = hasPermission(PERMISSIONS.TasksDelete);
  const canAssign = hasPermission(PERMISSIONS.TasksAssign);
  const canViewAll = hasPermission(PERMISSIONS.TasksView);

  const setTab = useCallback(
    (t: Tab) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        next.set('tab', t);
        return next;
      });
    },
    [setSearchParams]
  );

  const syncParamsToFilter = useCallback(() => {
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev);
      if (filter.status) next.set('status', filter.status);
      else next.delete('status');
      if (filter.priority) next.set('priority', filter.priority);
      else next.delete('priority');
      if (filter.assignedToUserId) next.set('assignedToUserId', filter.assignedToUserId);
      else next.delete('assignedToUserId');
      if (filter.search) next.set('search', filter.search);
      else next.delete('search');
      return next;
    });
  }, [filter, setSearchParams]);

  const loadTasks = useCallback(() => {
    setLoading(true);
    const load = tab === 'my' ? getTasksModuleMy(filter) : getTasksModuleList(filter);
    load
      .then((res) => {
        const data = res.data ?? (res as unknown as { data?: TasksStandaloneTask[] }).data;
        if (res.success && Array.isArray(data)) {
          setTasks(data);
        } else {
          addToast('error', res.error?.message ?? 'Failed to load tasks.');
          setTasks([]);
        }
      })
      .catch(() => {
        addToast('error', 'Failed to load tasks.');
        setTasks([]);
      })
      .finally(() => setLoading(false));
  }, [tab, filter]);

  useEffect(() => {
    loadTasks();
  }, [loadTasks]);

  useEffect(() => {
    syncParamsToFilter();
  }, [syncParamsToFilter]);

  useEffect(() => {
    getTenantAdminUsers().then((res) => {
      const data = res.data ?? (res as unknown as { data?: TenantAdminUser[] }).data;
      setAssigneeOptions(Array.isArray(data) ? data : []);
    });
  }, []);

  const handleToggleComplete = (task: TasksStandaloneTask) => (e: React.MouseEvent) => {
    e.stopPropagation();
    const newStatus = task.status === 'Completed' ? 'Open' : 'Completed';
    setTasksModuleStatus(task.id, newStatus).then((res) => {
      if (res.success && res.data) {
        setTasks((prev) => prev.map((t) => (t.id === task.id ? res.data! : t)));
        addToast('success', newStatus === 'Completed' ? 'Task completed.' : 'Task reopened.');
      } else {
        addToast('error', res.error?.message ?? 'Failed to update.');
      }
    });
  };

  const assigneeSelectOptions = assigneeOptions.map((u) => ({
    id: u.userId,
    label: u.displayName || u.email || u.userId,
  }));

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <h1 className="text-2xl font-semibold" style={{ color: 'var(--card-header-color, #323130)' }}>
          Tasks
        </h1>
        {canCreate && (
          <button
            type="button"
            onClick={() => setDrawerTaskId('new')}
            className="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-blue-600 text-white text-sm font-medium hover:bg-blue-700"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            Add Task
          </button>
        )}
      </div>

      <div className="flex gap-1 border-b border-gray-200 dark:border-slate-600">
        <button
          type="button"
          onClick={() => setTab('my')}
          className={`px-4 py-2 text-sm font-medium rounded-t-lg transition-colors ${
            tab === 'my'
              ? 'bg-primary-50 dark:bg-primary-900/20 text-primary-600 dark:text-primary-400 border-b-2 border-primary-500'
              : 'text-gray-600 dark:text-slate-400 hover:bg-gray-100 dark:hover:bg-slate-700'
          }`}
        >
          My Tasks
        </button>
        {canViewAll && (
          <button
            type="button"
            onClick={() => setTab('all')}
            className={`px-4 py-2 text-sm font-medium rounded-t-lg transition-colors ${
              tab === 'all'
                ? 'bg-primary-50 dark:bg-primary-900/20 text-primary-600 dark:text-primary-400 border-b-2 border-primary-500'
                : 'text-gray-600 dark:text-slate-400 hover:bg-gray-100 dark:hover:bg-slate-700'
            }`}
          >
            All Tasks
          </button>
        )}
      </div>

      <TaskFilterBar
        filter={filter}
        onChange={setFilter}
        showAssignee={tab === 'all'}
        assigneeOptions={assigneeSelectOptions}
      />

      {loading ? (
        <div className="animate-pulse space-y-3">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-20 bg-gray-100 dark:bg-slate-700 rounded-xl" />
          ))}
        </div>
      ) : tasks.length === 0 ? (
        <div
          className="rounded-xl border border-gray-100 dark:border-slate-700/50 bg-white dark:bg-slate-800/50 p-8 text-center text-sm"
          style={{ color: 'var(--card-description-color, #605e5c)' }}
        >
          {tab === 'my' ? 'No tasks assigned to you.' : 'No tasks match the filters.'}
        </div>
      ) : (
        <ul className="space-y-3">
          {tasks.map((task) => (
            <li key={task.id}>
              <TaskCard
                task={task}
                onSelect={() => setDrawerTaskId(task.id)}
                onToggleComplete={handleToggleComplete(task)}
                canEdit={canEdit}
              />
            </li>
          ))}
        </ul>
      )}

      <TaskDrawer
        taskId={drawerTaskId === 'new' ? null : drawerTaskId}
        open={drawerTaskId !== null}
        onClose={() => setDrawerTaskId(null)}
        onSaved={loadTasks}
        onDeleted={loadTasks}
        canCreate={canCreate}
        canEdit={canEdit}
        canDelete={canDelete}
        canAssign={canAssign}
        assigneeOptions={assigneeOptions}
      />
    </div>
  );
}
