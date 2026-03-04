import { useCallback, useEffect, useState } from 'react';
import { getProjectDashboard } from './dashboardService';
import { addToast } from '../../utils/toast';
import type { ProjectDashboard } from './types';

const STATUS_LABELS: Record<string, string> = { ToDo: 'Todo', InProgress: 'In Progress', Done: 'Done' };

interface ProjectDashboardTabProps {
  projectId: string;
}

function StatIconTasks() {
  return (
    <svg className="w-5 h-5 text-gray-500 dark:text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
    </svg>
  );
}

function StatIconCheck() {
  return (
    <svg className="w-5 h-5 text-emerald-500 dark:text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
    </svg>
  );
}

function StatIconAlert() {
  return (
    <svg className="w-5 h-5 text-amber-500 dark:text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
    </svg>
  );
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
          setData(null);
          addToast('error', res.error?.message ?? 'Failed to load dashboard.');
        }
      })
      .catch(() => {
        setData(null);
        addToast('error', 'Failed to load dashboard.');
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
            <div key={i} className="h-28 rounded-xl bg-gray-100 dark:bg-slate-700/80 border border-gray-200/80 dark:border-slate-600/80" />
          ))}
        </div>
        <div className="h-72 rounded-xl bg-gray-100 dark:bg-slate-700/80 border border-gray-200/80 dark:border-slate-600/80" />
        <div className="h-72 rounded-xl bg-gray-100 dark:bg-slate-700/80 border border-gray-200/80 dark:border-slate-600/80" />
      </div>
    );
  }

  if (!data) {
    return (
      <div className="rounded-xl border border-gray-200 dark:border-slate-700 bg-gray-50/50 dark:bg-slate-800/30 p-10 text-center">
        <p className="text-sm text-gray-500 dark:text-slate-400">Unable to load dashboard.</p>
      </div>
    );
  }

  const completedPct = data.totalTasks > 0 ? Math.round((data.completedTasks / data.totalTasks) * 100) : 0;
  const maxStatus = Math.max(1, ...data.tasksPerStatus.map((x) => x.count));
  const maxMember = Math.max(1, ...data.tasksPerMember.map((x) => x.count));

  return (
    <div className="space-y-8">
      {/* KPI cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="rounded-xl border border-gray-200/80 dark:border-slate-700/80 bg-white dark:bg-slate-800/50 p-5 shadow-sm">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500 dark:text-slate-400 uppercase tracking-wide">
                Total tasks
              </p>
              <p className="mt-1 text-2xl font-semibold text-gray-900 dark:text-slate-100 tabular-nums">
                {data.totalTasks}
              </p>
            </div>
            <div className="w-10 h-10 rounded-lg bg-gray-100 dark:bg-slate-700/80 flex items-center justify-center">
              <StatIconTasks />
            </div>
          </div>
        </div>
        <div className="rounded-xl border border-gray-200/80 dark:border-slate-700/80 bg-white dark:bg-slate-800/50 p-5 shadow-sm">
          <div className="flex items-center justify-between">
            <div className="min-w-0 flex-1">
              <p className="text-sm font-medium text-gray-500 dark:text-slate-400 uppercase tracking-wide">
                Completed
              </p>
              <p className="mt-1 text-2xl font-semibold text-gray-900 dark:text-slate-100 tabular-nums">
                {completedPct}%
              </p>
              <div className="mt-2 h-2 rounded-full bg-gray-100 dark:bg-slate-700 overflow-hidden">
                <div
                  className="h-full rounded-full bg-emerald-500 dark:bg-emerald-600 transition-all duration-500"
                  style={{ width: `${completedPct}%` }}
                />
              </div>
            </div>
            <div className="w-10 h-10 rounded-lg bg-emerald-50 dark:bg-emerald-900/20 flex items-center justify-center shrink-0 ml-3">
              <StatIconCheck />
            </div>
          </div>
        </div>
        <div className="rounded-xl border border-gray-200/80 dark:border-slate-700/80 bg-white dark:bg-slate-800/50 p-5 shadow-sm">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500 dark:text-slate-400 uppercase tracking-wide">
                Overdue
              </p>
              <p className={`mt-1 text-2xl font-semibold tabular-nums ${data.overdueTasks > 0 ? 'text-red-600 dark:text-red-400' : 'text-gray-900 dark:text-slate-100'}`}>
                {data.overdueTasks}
              </p>
            </div>
            <div className="w-10 h-10 rounded-lg bg-amber-50 dark:bg-amber-900/20 flex items-center justify-center">
              <StatIconAlert />
            </div>
          </div>
        </div>
      </div>

      {/* Tasks by status */}
      <section className="rounded-xl border border-gray-200/80 dark:border-slate-700/80 bg-white dark:bg-slate-800/50 p-5 shadow-sm">
        <h3 className="text-sm font-semibold text-gray-900 dark:text-slate-100 uppercase tracking-wide mb-4">
          Tasks by status
        </h3>
        <div className="space-y-4">
          {data.tasksPerStatus.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-slate-400 py-2">No tasks yet.</p>
          ) : (
            data.tasksPerStatus.map((item) => (
              <div key={item.status} className="flex items-center gap-4">
                <span className="w-28 text-sm font-medium text-gray-700 dark:text-slate-300 shrink-0">
                  {STATUS_LABELS[item.status] ?? item.status}
                </span>
                <div className="flex-1 h-9 rounded-lg bg-gray-100 dark:bg-slate-700/80 overflow-hidden min-w-0 flex items-center">
                  <div
                    className="h-full rounded-lg bg-primary-500 dark:bg-primary-600 transition-all duration-500 flex items-center justify-end pr-2 min-w-0"
                    style={{ width: `${(item.count / maxStatus) * 100}%`, minWidth: item.count > 0 ? '2.5rem' : 0 }}
                  >
                    {item.count > 0 && (
                      <span className="text-xs font-semibold text-white tabular-nums">{item.count}</span>
                    )}
                  </div>
                </div>
                <span className="text-sm font-medium text-gray-500 dark:text-slate-400 tabular-nums w-10 text-right">
                  {item.count}
                </span>
              </div>
            ))
          )}
        </div>
      </section>

      {/* Tasks by assignee */}
      <section className="rounded-xl border border-gray-200/80 dark:border-slate-700/80 bg-white dark:bg-slate-800/50 p-5 shadow-sm">
        <h3 className="text-sm font-semibold text-gray-900 dark:text-slate-100 uppercase tracking-wide mb-4">
          Tasks by assignee
        </h3>
        <div className="space-y-4">
          {data.tasksPerMember.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-slate-400 py-2">No assigned tasks.</p>
          ) : (
            data.tasksPerMember.map((item) => (
              <div key={item.userId} className="flex items-center gap-4">
                <span
                  className="w-40 text-sm font-medium text-gray-700 dark:text-slate-300 shrink-0 truncate"
                  title={item.userDisplayName ?? item.userId}
                >
                  {item.userDisplayName || 'Unassigned'}
                </span>
                <div className="flex-1 h-9 rounded-lg bg-gray-100 dark:bg-slate-700/80 overflow-hidden min-w-0 flex items-center">
                  <div
                    className="h-full rounded-lg bg-slate-500 dark:bg-slate-600 transition-all duration-500 flex items-center justify-end pr-2 min-w-0"
                    style={{ width: `${(item.count / maxMember) * 100}%`, minWidth: item.count > 0 ? '2.5rem' : 0 }}
                  >
                    {item.count > 0 && (
                      <span className="text-xs font-semibold text-white tabular-nums">{item.count}</span>
                    )}
                  </div>
                </div>
                <span className="text-sm font-medium text-gray-500 dark:text-slate-400 tabular-nums w-10 text-right">
                  {item.count}
                </span>
              </div>
            ))
          )}
        </div>
      </section>
    </div>
  );
}
