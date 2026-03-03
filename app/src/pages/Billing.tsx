import { useCallback, useEffect, useState } from 'react';
import {
  getSubscriptionUsage,
  getPlans,
  createCheckoutSession,
  type SubscriptionUsageResponse,
  type PlanResponse,
} from '../services/billingService';
import { addToast } from '../utils/toast';

function formatLimit(used: number, limit: number | null | undefined): string {
  if (limit == null) return `${used} (unlimited)`;
  return `${used} / ${limit}`;
}

function formatStorage(mb: number, limitMb: number | null | undefined): string {
  if (limitMb == null) return `${mb.toFixed(1)} MB used`;
  return `${mb.toFixed(1)} / ${limitMb} MB`;
}

function usagePercent(used: number, limit: number | null | undefined): number {
  if (limit == null || limit <= 0) return 0;
  return Math.min(100, Math.round((used / limit) * 100));
}

export function Billing() {
  const [usage, setUsage] = useState<SubscriptionUsageResponse | null>(null);
  const [plans, setPlans] = useState<PlanResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [checkoutPlanId, setCheckoutPlanId] = useState<string | null>(null);

  const load = useCallback(() => {
    setLoading(true);
    setError(null);
    Promise.allSettled([getSubscriptionUsage(), getPlans()])
      .then(([usageSettled, plansSettled]) => {
        // Usage
        if (usageSettled.status === 'fulfilled') {
          const usageRes = usageSettled.value;
          const u = usageRes.data ?? (usageRes as unknown as { data?: SubscriptionUsageResponse }).data;
          if (usageRes.success && u) setUsage(u);
          else if (!usageRes.success && usageRes.error?.message) setError(usageRes.error.message);
        } else {
          const err = usageSettled.reason;
          const data = err?.response?.data;
          const msg = data?.error?.message ?? data?.message ?? 'Failed to load billing information.';
          setError(msg);
          addToast('error', msg);
        }
        // Plans (load even if usage failed)
        if (plansSettled.status === 'fulfilled') {
          const plansRes = plansSettled.value;
          const p = plansRes.data ?? (plansRes as unknown as { data?: PlanResponse[] }).data;
          if (plansRes.success && Array.isArray(p)) setPlans(p);
        } else if (usageSettled.status === 'fulfilled') {
          addToast('error', 'Failed to load plans.');
        }
      })
      .catch(() => {
        setError('Failed to load billing information.');
        addToast('error', 'Failed to load billing.');
      })
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const currentPlanId = usage?.plan?.id ?? null;

  async function handleCheckout(plan: PlanResponse, billingInterval: 'monthly' | 'yearly') {
    const priceId = billingInterval === 'yearly' ? plan.stripePriceIdYearly : plan.stripePriceIdMonthly;
    if (!priceId) {
      addToast('error', 'This plan is not available for online checkout. Contact support.');
      return;
    }
    const base = typeof window !== 'undefined' ? window.location.origin : '';
    setCheckoutPlanId(plan.id);
    try {
      const res = await createCheckoutSession({
        planId: plan.id,
        billingInterval,
        successUrl: `${base}/billing/success`,
        cancelUrl: `${base}/billing/cancel`,
      });
      if (res.success && res.data?.url) {
        window.location.href = res.data.url;
        return;
      }
      addToast('error', res.error?.message ?? 'Could not start checkout.');
    } catch {
      addToast('error', 'Could not start checkout.');
    } finally {
      setCheckoutPlanId(null);
    }
  }

  if (loading && !usage) {
    return (
      <div className="space-y-6">
        <div className="page-header">
          <h1>Billing</h1>
          <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">Your subscription and usage.</p>
        </div>
        <div className="animate-pulse rounded-xl border border-gray-200 dark:border-[#2d3d5c] bg-white dark:bg-[#1e2a4a] p-8">
          <div className="h-6 w-48 bg-gray-200 dark:bg-gray-700 rounded mb-4" />
          <div className="h-12 bg-gray-200 dark:bg-gray-700 rounded mb-4" />
          <div className="h-12 bg-gray-200 dark:bg-gray-700 rounded" />
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div className="page-header">
        <h1 className="text-2xl font-bold tracking-tight text-gray-900 dark:text-white">Billing</h1>
        <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
          Your subscription plan and usage. Upgrade to unlock more projects, members, and storage.
        </p>
      </div>

      {error && (
        <div className="rounded-lg border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20 px-4 py-3 text-sm text-red-700 dark:text-red-300">
          {error}
        </div>
      )}

      <div className="rounded-xl border border-gray-200 dark:border-gray-700/60 bg-white dark:bg-gray-800/80 shadow-sm overflow-hidden">
        <div className="p-6 border-b border-gray-200 dark:border-gray-700/60 bg-gray-50/50 dark:bg-gray-900/40">
          <h2 className="text-base font-semibold text-gray-900 dark:text-white">Current plan</h2>
          {usage?.plan ? (
            <div className="mt-3 flex flex-wrap items-center gap-4">
              <span className="text-xl font-semibold text-gray-900 dark:text-white">{usage.plan.name}</span>
              {usage.status && (
                <span className="inline-flex items-center rounded-full bg-emerald-100 dark:bg-emerald-900/40 px-2.5 py-0.5 text-xs font-medium text-emerald-800 dark:text-emerald-200">
                  {usage.status}
                </span>
              )}
              {usage.plan.priceMonthly != null && usage.plan.priceMonthly > 0 && (
                <span className="text-sm text-gray-500 dark:text-gray-400">
                  ${usage.plan.priceMonthly}/mo
                  {usage.plan.priceYearly != null && usage.plan.priceYearly > 0 && (
                    <> · ${usage.plan.priceYearly}/yr</>
                  )}
                </span>
              )}
              <a href="#plans" className="inline-flex items-center rounded-lg bg-primary-500 px-3 py-1.5 text-sm font-medium text-white shadow-sm hover:bg-primary-600 dark:bg-primary-600 dark:hover:bg-primary-500">
                Change plan
              </a>
            </div>
          ) : (
            <p className="mt-2 text-sm text-gray-500 dark:text-gray-400">
              {usage?.status === 'None' ? 'No active subscription.' : 'Loading…'}
            </p>
          )}
        </div>

        <div className="p-6">
          <h2 className="text-base font-semibold text-gray-900 dark:text-white mb-4">Usage overview</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
            {/* Projects */}
            <div className="rounded-lg border border-gray-200 dark:border-gray-700/60 bg-gray-50/50 dark:bg-gray-900/30 p-4">
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm font-medium text-gray-600 dark:text-gray-300">Projects</span>
                <span className="text-sm font-semibold tabular-nums text-gray-900 dark:text-white">
                  {formatLimit(usage?.projectsUsed ?? 0, usage?.projectsLimit)}
                </span>
              </div>
              <div className="h-2 w-full rounded-full bg-gray-200 dark:bg-gray-700 overflow-hidden">
                <div
                  className="h-full rounded-full bg-primary-500 dark:bg-primary-400 transition-all duration-300"
                  style={{ width: `${usagePercent(usage?.projectsUsed ?? 0, usage?.projectsLimit ?? undefined)}%` }}
                />
              </div>
            </div>
            {/* Members */}
            <div className="rounded-lg border border-gray-200 dark:border-gray-700/60 bg-gray-50/50 dark:bg-gray-900/30 p-4">
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm font-medium text-gray-600 dark:text-gray-300">Members</span>
                <span className="text-sm font-semibold tabular-nums text-gray-900 dark:text-white">
                  {formatLimit(usage?.membersUsed ?? 0, usage?.membersLimit)}
                </span>
              </div>
              <div className="h-2 w-full rounded-full bg-gray-200 dark:bg-gray-700 overflow-hidden">
                <div
                  className="h-full rounded-full bg-primary-500 dark:bg-primary-400 transition-all duration-300"
                  style={{ width: `${usagePercent(usage?.membersUsed ?? 0, usage?.membersLimit ?? undefined)}%` }}
                />
              </div>
            </div>
            {/* Storage */}
            <div className="rounded-lg border border-gray-200 dark:border-gray-700/60 bg-gray-50/50 dark:bg-gray-900/30 p-4">
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm font-medium text-gray-600 dark:text-gray-300">Storage</span>
                <span className="text-sm font-semibold tabular-nums text-gray-900 dark:text-white">
                  {formatStorage(usage?.storageUsedMB ?? 0, usage?.storageLimitMB ?? undefined)}
                </span>
              </div>
              <div className="h-2 w-full rounded-full bg-gray-200 dark:bg-gray-700 overflow-hidden">
                <div
                  className="h-full rounded-full bg-primary-500 dark:bg-primary-400 transition-all duration-300"
                  style={{ width: `${usagePercent(usage?.storageUsedMB ?? 0, usage?.storageLimitMB ?? undefined)}%` }}
                />
              </div>
            </div>
            {/* Time logs */}
            <div className="rounded-lg border border-gray-200 dark:border-gray-700/60 bg-gray-50/50 dark:bg-gray-900/30 p-4">
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm font-medium text-gray-600 dark:text-gray-300">Time logs</span>
                <span className="text-sm font-semibold tabular-nums text-gray-900 dark:text-white">
                  {(usage?.timeLogsUsed ?? 0).toLocaleString()} entries
                </span>
              </div>
              <div className="h-2 w-full rounded-full bg-gray-200 dark:bg-gray-700">
                <div className="h-full rounded-full bg-gray-400 dark:bg-gray-500 w-0" aria-hidden />
              </div>
              <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">Tracked for reporting</p>
            </div>
          </div>
          {usage?.automationEnabled === false && usage?.plan && (
            <p className="mt-4 text-sm text-amber-600 dark:text-amber-400">
              Automation rules are not included in your current plan. Upgrade to enable them.
            </p>
          )}
        </div>
      </div>

      <div id="plans" className="rounded-xl border border-gray-200 dark:border-gray-700/60 bg-white dark:bg-gray-800/80 shadow-sm overflow-hidden">
        <div className="p-6 border-b border-gray-200 dark:border-gray-700/60">
          <h2 className="text-base font-semibold text-gray-900 dark:text-white">Available plans</h2>
          <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
            Choose a plan and complete payment securely with Stripe.
          </p>
        </div>
        <div className="p-6 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {plans.map((plan) => {
            const isCurrent = currentPlanId === plan.id;
            const canCheckoutMonthly = !!plan.stripePriceIdMonthly;
            const canCheckoutYearly = !!plan.stripePriceIdYearly;
            const canCheckout = canCheckoutMonthly || canCheckoutYearly;
            const loadingCheckout = checkoutPlanId === plan.id;

            return (
              <div
                key={plan.id}
                className={`rounded-xl border p-5 transition-shadow ${
                  isCurrent
                    ? 'border-primary-500 dark:border-primary-400 bg-primary-50/50 dark:bg-primary-900/20 shadow-md'
                    : 'border-gray-200 dark:border-gray-700/60 bg-gray-50/30 dark:bg-gray-900/30 hover:shadow-sm'
                }`}
              >
                <div className="flex items-center justify-between">
                  <h3 className="font-semibold text-gray-900 dark:text-white">{plan.name}</h3>
                  {isCurrent && (
                    <span className="inline-flex items-center rounded-full bg-primary-100 dark:bg-primary-900/50 px-2.5 py-0.5 text-xs font-medium text-primary-800 dark:text-primary-200">
                      Current
                    </span>
                  )}
                </div>
                <div className="mt-3 text-sm text-gray-600 dark:text-gray-300 space-y-1">
                  {plan.priceMonthly != null && plan.priceMonthly >= 0 && (
                    <p>${plan.priceMonthly}/mo</p>
                  )}
                  {plan.priceYearly != null && plan.priceYearly > 0 && (
                    <p>${plan.priceYearly}/yr</p>
                  )}
                  <p className="mt-1">Projects: {plan.maxProjects == null ? 'Unlimited' : plan.maxProjects}</p>
                  <p>Members: {plan.maxUsers == null ? 'Unlimited' : plan.maxUsers}</p>
                  <p>Storage: {plan.storageLimitMB == null ? 'Unlimited' : `${plan.storageLimitMB} MB`}</p>
                  <p>Automation: {plan.automationEnabled ? 'Yes' : 'No'}</p>
                </div>
                <div className="mt-4 flex flex-wrap gap-2">
                  {isCurrent ? (
                    <span className="text-sm text-gray-500 dark:text-gray-400">Your current plan</span>
                  ) : canCheckout ? (
                    <>
                      {canCheckoutMonthly && (
                        <button
                          type="button"
                          onClick={() => handleCheckout(plan, 'monthly')}
                          disabled={loadingCheckout}
                          className="btn-primary text-sm py-1.5 px-3"
                        >
                          {loadingCheckout ? 'Redirecting…' : 'Subscribe (monthly)'}
                        </button>
                      )}
                      {canCheckoutYearly && (
                        <button
                          type="button"
                          onClick={() => handleCheckout(plan, 'yearly')}
                          disabled={loadingCheckout}
                          className="btn-secondary text-sm py-1.5 px-3"
                        >
                          {loadingCheckout ? '…' : 'Subscribe (yearly)'}
                        </button>
                      )}
                    </>
                  ) : (
                    <span className="text-sm text-gray-500 dark:text-gray-400">Contact support</span>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
