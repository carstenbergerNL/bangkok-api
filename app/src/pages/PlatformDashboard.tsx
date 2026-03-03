import { useCallback, useEffect, useState } from 'react';
import {
  getDashboardStats,
  getTenants,
  getTenantUsage,
  suspendTenant,
  setTenantStatus,
  upgradeTenant,
} from '../services/platformAdminService';
import { getPlans } from '../services/billingService';
import type {
  PlatformDashboardStatsResponse,
  PlatformTenantListItemResponse,
  TenantUsageDetailResponse,
} from '../models/PlatformAdmin';
import type { PlanResponse } from '../services/billingService';
import { addToast } from '../utils/toast';

function StatCard({
  label,
  value,
  sub,
}: {
  label: string;
  value: string | number;
  sub?: string;
}) {
  return (
    <div className="rounded-xl border border-gray-200 dark:border-[#2d3d5c] bg-white dark:bg-[#1e2a4a] p-5 shadow-sm">
      <p className="text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">
        {label}
      </p>
      <p className="mt-1 text-2xl font-semibold text-gray-900 dark:text-white">{value}</p>
      {sub != null && <p className="mt-0.5 text-sm text-gray-500 dark:text-gray-400">{sub}</p>}
    </div>
  );
}

export function PlatformDashboard() {
  const [stats, setStats] = useState<PlatformDashboardStatsResponse | null>(null);
  const [tenants, setTenants] = useState<PlatformTenantListItemResponse[]>([]);
  const [plans, setPlans] = useState<PlanResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [usageModal, setUsageModal] = useState<TenantUsageDetailResponse | null>(null);
  const [upgradeTenantId, setUpgradeTenantId] = useState<string | null>(null);
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  const load = useCallback(() => {
    setLoading(true);
    setError(null);
    Promise.all([
      getDashboardStats(),
      getTenants(),
      getPlans(),
    ])
      .then(([statsRes, tenantsRes, plansRes]) => {
        if (statsRes.success && statsRes.data) setStats(statsRes.data);
        if (tenantsRes.success && Array.isArray(tenantsRes.data)) setTenants(tenantsRes.data);
        if (plansRes.success && Array.isArray(plansRes.data)) setPlans(plansRes.data);
        const err = !statsRes.success ? statsRes.error?.message : !tenantsRes.success ? tenantsRes.error?.message : null;
        if (err) setError(err);
      })
      .catch((err) => {
        const msg = err.response?.data?.error?.message ?? err.response?.data?.message ?? err.message ?? 'Failed to load platform data.';
        setError(msg);
        addToast('error', msg);
      })
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function handleSuspend(tenantId: string) {
    setActionLoading(tenantId);
    try {
      const res = await suspendTenant(tenantId);
      if (res.success) {
        addToast('success', 'Tenant suspended.');
        load();
      } else {
        addToast('error', res.error?.message ?? 'Failed to suspend.');
      }
    } catch {
      addToast('error', 'Failed to suspend tenant.');
    } finally {
      setActionLoading(null);
    }
  }

  async function handleSetStatus(tenantId: string, status: string) {
    setActionLoading(tenantId);
    try {
      const res = await setTenantStatus(tenantId, { status });
      if (res.success) {
        addToast('success', `Tenant status set to ${status}.`);
        load();
      } else {
        addToast('error', res.error?.message ?? 'Failed to update status.');
      }
    } catch {
      addToast('error', 'Failed to update status.');
    } finally {
      setActionLoading(null);
    }
  }

  async function handleUpgrade(tenantId: string, planId: string) {
    setActionLoading(tenantId);
    try {
      const res = await upgradeTenant(tenantId, { planId });
      if (res.success) {
        addToast('success', 'Tenant plan updated.');
        setUpgradeTenantId(null);
        load();
      } else {
        addToast('error', res.error?.message ?? 'Failed to upgrade.');
      }
    } catch {
      addToast('error', 'Failed to upgrade tenant.');
    } finally {
      setActionLoading(null);
    }
  }

  async function handleViewUsage(tenantId: string) {
    try {
      const res = await getTenantUsage(tenantId);
      if (res.success && res.data) setUsageModal(res.data);
      else addToast('error', res.error?.message ?? 'Failed to load usage.');
    } catch {
      addToast('error', 'Failed to load usage.');
    }
  }

  if (loading && !stats) {
    return (
      <div className="space-y-6">
        <div className="page-header">
          <h1>Platform Admin Dashboard</h1>
          <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">Loading…</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div className="page-header">
        <h1>Platform Admin Dashboard</h1>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          View tenants, subscriptions, revenue, and manage tenant status and plans.
        </p>
      </div>

      {error && (
        <div className="rounded-lg border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20 px-4 py-3 text-sm text-red-700 dark:text-red-300">
          {error}
        </div>
      )}

      {/* Stats */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
        <StatCard label="Total tenants" value={stats?.totalTenants ?? 0} />
        <StatCard label="Active subscriptions" value={stats?.activeSubscriptions ?? 0} />
        <StatCard
          label="Monthly recurring revenue"
          value={stats != null ? `$${Number(stats.monthlyRecurringRevenue).toFixed(2)}` : '—'}
        />
        <StatCard label="Trial users" value={stats?.trialUsers ?? 0} />
        <StatCard label="Churned users" value={stats?.churnedUsers ?? 0} />
      </div>

      {/* Tenants table */}
      <div className="rounded-xl border border-gray-200 dark:border-[#2d3d5c] bg-white dark:bg-[#1e2a4a] shadow-sm overflow-hidden">
        <div className="border-b border-gray-200 dark:border-[#2d3d5c] px-6 py-4">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-white">Tenants</h2>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
            Suspend, resume, upgrade plan, or view usage.
          </p>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-[#2d3d5c]">
            <thead>
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Name</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Slug</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Status</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Plan</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Sub status</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Projects</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Users</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Storage (MB)</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Time logs</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-[#2d3d5c]">
              {tenants.length === 0 && (
                <tr>
                  <td colSpan={10} className="px-6 py-8 text-center text-sm text-gray-500 dark:text-gray-400">
                    No tenants.
                  </td>
                </tr>
              )}
              {tenants.map((t) => (
                <tr key={t.id} className="bg-white dark:bg-[#1e2a4a] hover:bg-gray-50 dark:hover:bg-[#253558]">
                  <td className="px-6 py-3 text-sm font-medium text-gray-900 dark:text-white whitespace-nowrap">{t.name}</td>
                  <td className="px-6 py-3 text-sm text-gray-600 dark:text-gray-300 font-mono">{t.slug}</td>
                  <td className="px-6 py-3 whitespace-nowrap">
                    <span
                      className={
                        t.status === 'Active'
                          ? 'text-green-600 dark:text-green-400'
                          : t.status === 'Suspended'
                            ? 'text-amber-600 dark:text-amber-400'
                            : 'text-gray-600 dark:text-gray-400'
                      }
                    >
                      {t.status}
                    </span>
                  </td>
                  <td className="px-6 py-3 text-sm text-gray-600 dark:text-gray-300">{t.planName ?? '—'}</td>
                  <td className="px-6 py-3 text-sm text-gray-600 dark:text-gray-300">{t.subscriptionStatus ?? '—'}</td>
                  <td className="px-6 py-3 text-sm text-gray-600 dark:text-gray-300 text-right">{t.projectsCount}</td>
                  <td className="px-6 py-3 text-sm text-gray-600 dark:text-gray-300 text-right">{t.usersCount}</td>
                  <td className="px-6 py-3 text-sm text-gray-600 dark:text-gray-300 text-right">{Number(t.storageUsedMB).toFixed(1)}</td>
                  <td className="px-6 py-3 text-sm text-gray-600 dark:text-gray-300 text-right">{t.timeLogsCount}</td>
                  <td className="px-6 py-3 text-right whitespace-nowrap">
                    <div className="flex items-center justify-end gap-1">
                      <button
                        type="button"
                        onClick={() => handleViewUsage(t.id)}
                        className="text-sm text-[#0078d4] dark:text-[#4da9ff] hover:underline"
                      >
                        Usage
                      </button>
                      {t.status === 'Active' ? (
                        <button
                          type="button"
                          onClick={() => handleSuspend(t.id)}
                          disabled={actionLoading !== null}
                          className="text-sm text-amber-600 dark:text-amber-400 hover:underline disabled:opacity-50"
                        >
                          Suspend
                        </button>
                      ) : (
                        <button
                          type="button"
                          onClick={() => handleSetStatus(t.id, 'Active')}
                          disabled={actionLoading !== null}
                          className="text-sm text-green-600 dark:text-green-400 hover:underline disabled:opacity-50"
                        >
                          Resume
                        </button>
                      )}
                      {upgradeTenantId === t.id ? (
                        <span className="flex items-center gap-1">
                          <select
                            className="text-sm border border-gray-300 dark:border-[#2d3d5c] rounded bg-white dark:bg-[#1e2a4a] text-gray-900 dark:text-white px-2 py-1"
                            onChange={(e) => {
                              const id = e.target.value;
                              if (id) {
                                handleUpgrade(t.id, id);
                              }
                            }}
                            onBlur={() => setUpgradeTenantId(null)}
                            autoFocus
                          >
                            <option value="">Select plan</option>
                            {plans.map((p) => (
                              <option key={p.id} value={p.id}>
                                {p.name}
                              </option>
                            ))}
                          </select>
                          <button
                            type="button"
                            onClick={() => setUpgradeTenantId(null)}
                            className="text-sm text-gray-500 hover:underline"
                          >
                            Cancel
                          </button>
                        </span>
                      ) : (
                        <button
                          type="button"
                          onClick={() => setUpgradeTenantId(t.id)}
                          disabled={actionLoading !== null}
                          className="text-sm text-[#0078d4] dark:text-[#4da9ff] hover:underline disabled:opacity-50"
                        >
                          Upgrade
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Usage modal */}
      {usageModal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
          onClick={() => setUsageModal(null)}
          role="dialog"
          aria-modal="true"
          aria-labelledby="usage-modal-title"
        >
          <div
            className="rounded-xl border border-gray-200 dark:border-[#2d3d5c] bg-white dark:bg-[#1e2a4a] shadow-xl max-w-md w-full p-6"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 id="usage-modal-title" className="text-lg font-semibold text-gray-900 dark:text-white">
              Usage: {usageModal.tenantName}
            </h2>
            <p className="text-sm text-gray-500 dark:text-gray-400 mt-0.5 font-mono">{usageModal.slug}</p>
            <dl className="mt-4 space-y-2 text-sm">
              <div className="flex justify-between">
                <dt className="text-gray-500 dark:text-gray-400">Status</dt>
                <dd className="text-gray-900 dark:text-white">{usageModal.status}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-gray-500 dark:text-gray-400">Projects</dt>
                <dd className="text-gray-900 dark:text-white">{usageModal.projectsCount}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-gray-500 dark:text-gray-400">Users</dt>
                <dd className="text-gray-900 dark:text-white">{usageModal.usersCount}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-gray-500 dark:text-gray-400">Storage (MB)</dt>
                <dd className="text-gray-900 dark:text-white">{usageModal.storageUsedMB.toFixed(1)}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-gray-500 dark:text-gray-400">Time logs</dt>
                <dd className="text-gray-900 dark:text-white">{usageModal.timeLogsCount}</dd>
              </div>
            </dl>
            <div className="mt-6 flex justify-end">
              <button
                type="button"
                onClick={() => setUsageModal(null)}
                className="rounded-lg bg-gray-200 dark:bg-[#2d3d5c] px-4 py-2 text-sm font-medium text-gray-900 dark:text-white hover:bg-gray-300 dark:hover:bg-[#3b4a6a]"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
