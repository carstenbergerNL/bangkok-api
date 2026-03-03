import { useCallback, useEffect, useState } from 'react';
import { getProjectDashboard } from './dashboardService';
import { addToast } from '../../utils/toast';
import type { ProjectDashboard } from './types';

const STATUS_LABELS: Record<string, string> = { ToDo: 'Todo', InProgress: 'In Progress', Done: 'Done' };

interface ProjectDashboardTabProps {
  projectId: string;
}

export function ProjectDashboardTab({ projectId }: ProjectDashboardTabProps) {
  const [data, setData] = useState<ProjectDashboard | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(() => {
    setLoading(true);
    getProjectDashboard(projectId)
      .then((res) => {
        const d = res.data ?? (res as unknown as { Data?: ProjectDashboard }).Data;
        if (res.success && d) {
          setData(d);
        } else {
          addToast('error', res.error?.message ?? 'Failed to load dashboard.');
        }
      })
      .finally(() => setLoading(false));
  }, [projectId]);

  useEffect(() => {
    load();
  }, [load]);

  if (loading) {
    return (
      <div className="animate-pulse space-y-6">
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-24 bg-gray-100 dark:bg-slate-700 rounded-xl" />
          ))}
        </div>
        <div className="h-64 bg-gray-100 dark:bg-slate-700 rounded-xl" />
        <div className="h-64 bg-gray-100 dark:bg-slate-700 rounded-xl" />
      </div>
    );
  }

  if (!data) {
    return (
      <div className="rounded-xl border border-gray-100 dark:border-slate-700/50 p-8 text-center" style={{ color: 'var(--card-description-color, #605e5c)' }}>
        Unable to load dashboard.
      </div>
    );
  }

  const completedPct = data.totalTasks > 0 ? Math.round((data.completedTasks / data.totalTasks) * 100) : 0;
  const maxStatus = Math.max(1, ...data.tasksPerStatus.map((x) => x.count));
  const maxMember = Math.max(1, ...data.tasksPerMember.map((x) => x.count));

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="rounded-xl border border-gray-100 dark:border-slate-700/50 bg-white dark:bg-slate-800/50 p-5 shadow-sm">
          <p className="text-sm font-medium" style={{ color: 'var(--card-description-color, #605e5c)' }}>
            Total tasks
          </p>
          <p className="mt-1 text-2xl font-semibold" style={{ color: 'var(--card-header-color, #323130)' }}>
            {data.totalTasks}
          </p>
        </div>
        <div className="rounded-xl border border-gray-100 dark:border-slate-700/50 bg-white dark:bg-slate-800/50 p-5 shadow-sm">
          <p className="text-sm font-medium" style={{ color: 'var(--card-description-color, #605e5c)' }}>
            Completed
          </p>
          <p className="mt-1 text-2xl font-semibold" style={{ color: 'var(--card-header-color, #323130)' }}>
            {completedPct}%
          </p>
          <div className="mt-2 h-2 rounded-full bg-gray-100 dark:bg-slate-700 overflow-hidden">
            <div
              className="h-full rounded-full bg-emerald-500 dark:bg-emerald-600 transition-all duration-500"
              style={{ width: `${completedPct}%` }}
            />
          </div>
        </div>
        <div className="rounded-xl border border-gray-100 dark:border-slate-700/50 bg-white dark:bg-slate-800/50 p-5 shadow-sm">
          <p className="text-sm font-medium" style={{ color: 'var(--card-description-color, #605e5c)' }}>
            Overdue
          </p>
          <p className="mt-1 text-2xl font-semibold" style={{ color: data.overdueTasks > 0 ? 'var(--red-600, #dc2626)' : 'var(--card-header-color, #323130)' }}>
            {data.overdueTasks}
          </p>
        </div>
      </div>

      <div className="rounded-xl border border-gray-100 dark:border-slate-700/50 bg-white dark:bg-slate-800/50 p-5 shadow-sm">
        <h3 className="text-sm font-semibold mb-4" style={{ color: 'var(--card-header-color, #323130)' }}>
          Tasks by status
        </h3>
        <div className="space-y-3">
          {data.tasksPerStatus.length === 0 ? (
            <p className="text-sm" style={{ color: 'var(--card-description-color, #605e5c)' }}>
              No tasks yet.
            </p>
          ) : (
            data.tasksPerStatus.map((item) => (
              <div key={item.status} className="flex items-center gap-3">
                <span className="w-24 text-sm font-medium shrink-0" style={{ color: 'var(--card-header-color, #323130)' }}>
                  {STATUS_LABELS[item.status] ?? item.status}
                </span>
                <div className="flex-1 h-8 rounded-lg bg-gray-100 dark:bg-slate-700 overflow-hidden min-w-0">
                  <div
                    className="h-full rounded-lg bg-blue-500 dark:bg-blue-600 transition-all duration-500 flex items-center justify-end pr-2"
                    style={{ width: `${(item.count / maxStatus) * 100}%`, minWidth: item.count > 0 ? '2rem' : 0 }}
                  >
                    {item.count > 0 && (
                      <span className="text-xs font-medium text-white">{item.count}</span>
                    )}
                  </div>
                </div>
                <span className="text-sm tabular-nums w-8 text-right" style={{ color: 'var(--card-description-color, #605e5c)' }}>
                  {item.count}
                </span>
              </div>
            ))
          )}
        </div>
      </div>

      <div className="rounded-xl border border-gray-100 dark:border-slate-700/50 bg-white dark:bg-slate-800/50 p-5 shadow-sm">
        <h3 className="text-sm font-semibold mb-4" style={{ color: 'var(--card-header-color, #323130)' }}>
          Tasks by assignee
        </h3>
        <div className="space-y-3">
          {data.tasksPerMember.length === 0 ? (
            <p className="text-sm" style={{ color: 'var(--card-description-color, #605e5c)' }}>
              No assigned tasks.
            </p>
          ) : (
            data.tasksPerMember.map((item) => (
              <div key={item.userId} className="flex items-center gap-3">
                <span className="w-40 text-sm font-medium shrink-0 truncate" style={{ color: 'var(--card-header-color, #323130)' }} title={item.userDisplayName ?? item.userId}>
                  {item.userDisplayName || 'Unnamed'}
                </span>
                <div className="flex-1 h-8 rounded-lg bg-gray-100 dark:bg-slate-700 overflow-hidden min-w-0">
                  <div
                    className="h-full rounded-lg bg-slate-500 dark:bg-slate-600 transition-all duration-500 flex items-center justify-end pr-2"
                    style={{ width: `${(item.count / maxMember) * 100}%`, minWidth: item.count > 0 ? '2rem' : 0 }}
                  >
                    {item.count > 0 && (
                      <span className="text-xs font-medium text-white">{item.count}</span>
                    )}
                  </div>
                </div>
                <span className="text-sm tabular-nums w-8 text-right" style={{ color: 'var(--card-description-color, #605e5c)' }}>
                  {item.count}
                </span>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}
