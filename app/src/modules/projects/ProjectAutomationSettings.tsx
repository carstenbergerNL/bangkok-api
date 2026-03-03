import { useCallback, useEffect, useState } from 'react';
import { getAutomationRules, createAutomationRule, deleteAutomationRule } from './projectService';
import { getLabels } from './labelService';
import { getProjectMembers } from './memberService';
import { addToast } from '../../utils/toast';
import type { ProjectAutomationRule, CreateProjectAutomationRuleRequest, Label, ProjectMember } from './types';

const TRIGGERS = [
  { value: 'TaskCompleted', label: 'Task completed' },
  { value: 'TaskOverdue', label: 'Task overdue' },
  { value: 'TaskAssigned', label: 'Task assigned' },
] as const;

const ACTIONS = [
  { value: 'NotifyUser', label: 'Notify user' },
  { value: 'ChangeStatus', label: 'Change status' },
  { value: 'AddLabel', label: 'Add label' },
] as const;

const STATUS_OPTIONS = ['ToDo', 'In Progress', 'Done'];

interface ProjectAutomationSettingsProps {
  projectId: string;
}

export function ProjectAutomationSettings({ projectId }: ProjectAutomationSettingsProps) {
  const [rules, setRules] = useState<ProjectAutomationRule[]>([]);
  const [labels, setLabels] = useState<Label[]>([]);
  const [members, setMembers] = useState<ProjectMember[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const [trigger, setTrigger] = useState<string>(TRIGGERS[0].value);
  const [action, setAction] = useState<string>(ACTIONS[0].value);
  const [targetUserId, setTargetUserId] = useState<string>('');
  const [targetValue, setTargetValue] = useState<string>('');

  const loadRules = useCallback(() => {
    getAutomationRules(projectId)
      .then((res) => {
        const data = res.data ?? (res as unknown as { Data?: ProjectAutomationRule[] }).Data;
        if (res.success && Array.isArray(data)) setRules(data);
        else addToast('error', res.error?.message ?? 'Failed to load rules.');
      })
      .catch(() => addToast('error', 'Failed to load rules.'));
  }, [projectId]);

  const loadAll = useCallback(() => {
    setLoading(true);
    Promise.all([
      getAutomationRules(projectId),
      getLabels(projectId),
      getProjectMembers(projectId),
    ])
      .then(([rulesRes, labelsRes, membersRes]) => {
        const rulesData = rulesRes.data ?? (rulesRes as unknown as { Data?: ProjectAutomationRule[] }).Data;
        const labelsData = labelsRes.data ?? (labelsRes as unknown as { Data?: Label[] }).Data;
        const membersData = membersRes.data ?? (membersRes as unknown as { Data?: ProjectMember[] }).Data;
        if (rulesRes.success && Array.isArray(rulesData)) setRules(rulesData);
        if (labelsRes.success && Array.isArray(labelsData)) setLabels(labelsData);
        if (membersRes.success && Array.isArray(membersData)) setMembers(membersData);
      })
      .catch(() => addToast('error', 'Failed to load data.'))
      .finally(() => setLoading(false));
  }, [projectId]);

  useEffect(() => {
    loadAll();
  }, [loadAll]);

  const handleAdd = async (e: React.FormEvent) => {
    e.preventDefault();
    const request: CreateProjectAutomationRuleRequest = {
      trigger: trigger.trim(),
      action: action.trim(),
    };
    if (action === 'NotifyUser') {
      if (!targetUserId.trim()) {
        addToast('error', 'Select a user to notify.');
        return;
      }
      request.targetUserId = targetUserId.trim();
    }
    if (action === 'ChangeStatus') {
      if (!targetValue.trim()) {
        addToast('error', 'Select a status.');
        return;
      }
      request.targetValue = targetValue.trim();
    }
    if (action === 'AddLabel') {
      if (!targetValue.trim()) {
        addToast('error', 'Select a label.');
        return;
      }
      request.targetValue = targetValue.trim();
    }
    setSaving(true);
    try {
      const res = await createAutomationRule(projectId, request);
      if (res.success && res.data) {
        setRules((prev) => [...prev, res.data!]);
        setTrigger(TRIGGERS[0].value);
        setAction(ACTIONS[0].value);
        setTargetUserId('');
        setTargetValue('');
        addToast('success', 'Rule added.');
      } else {
        addToast('error', res.error?.message ?? 'Failed to add rule.');
      }
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (ruleId: string) => {
    setDeletingId(ruleId);
    try {
      const res = await deleteAutomationRule(projectId, ruleId);
      if (res.success) {
        setRules((prev) => prev.filter((r) => r.id !== ruleId));
        addToast('success', 'Rule removed.');
      } else {
        addToast('error', res.error?.message ?? 'Failed to remove rule.');
      }
    } finally {
      setDeletingId(null);
    }
  };

  const actionLabel = (a: string) => ACTIONS.find((x) => x.value === a)?.label ?? a;
  const triggerLabel = (t: string) => TRIGGERS.find((x) => x.value === t)?.label ?? t;
  const resolveTarget = (r: ProjectAutomationRule) => {
    if (r.action === 'NotifyUser' && r.targetUserId) {
      const m = members.find((x) => x.userId === r.targetUserId);
      return m ? m.userDisplayName || m.userEmail : r.targetUserId;
    }
    if (r.action === 'ChangeStatus' && r.targetValue) return `Status: ${r.targetValue}`;
    if (r.action === 'AddLabel' && r.targetValue) {
      const l = labels.find((x) => x.id === r.targetValue);
      return l ? l.name : r.targetValue;
    }
    return '';
  };

  if (loading) {
    return (
      <div className="animate-pulse space-y-3">
        <div className="h-6 w-48 bg-gray-200 dark:bg-slate-600 rounded" />
        <div className="h-12 bg-gray-100 dark:bg-slate-700 rounded-xl" />
        <div className="h-12 bg-gray-100 dark:bg-slate-700 rounded-xl" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-sm font-medium text-gray-700 dark:text-slate-300 mb-2">Add rule</h3>
        <form onSubmit={handleAdd} className="flex flex-wrap items-end gap-3">
          <div className="min-w-[140px]">
            <label className="block text-xs text-gray-500 dark:text-slate-400 mb-1">When</label>
            <select
              value={trigger}
              onChange={(e) => setTrigger(e.target.value)}
              className="w-full rounded-lg border border-gray-300 dark:border-slate-600 bg-white dark:bg-slate-800 text-gray-900 dark:text-slate-100 px-3 py-2 text-sm"
            >
              {TRIGGERS.map((t) => (
                <option key={t.value} value={t.value}>
                  {t.label}
                </option>
              ))}
            </select>
          </div>
          <div className="min-w-[140px]">
            <label className="block text-xs text-gray-500 dark:text-slate-400 mb-1">Action</label>
            <select
              value={action}
              onChange={(e) => {
                setAction(e.target.value);
                setTargetUserId('');
                setTargetValue('');
              }}
              className="w-full rounded-lg border border-gray-300 dark:border-slate-600 bg-white dark:bg-slate-800 text-gray-900 dark:text-slate-100 px-3 py-2 text-sm"
            >
              {ACTIONS.map((a) => (
                <option key={a.value} value={a.value}>
                  {a.label}
                </option>
              ))}
            </select>
          </div>
          {action === 'NotifyUser' && (
            <div className="min-w-[160px]">
              <label className="block text-xs text-gray-500 dark:text-slate-400 mb-1">User</label>
              <select
                value={targetUserId}
                onChange={(e) => setTargetUserId(e.target.value)}
                className="w-full rounded-lg border border-gray-300 dark:border-slate-600 bg-white dark:bg-slate-800 text-gray-900 dark:text-slate-100 px-3 py-2 text-sm"
              >
                <option value="">Select user</option>
                {members.map((m) => (
                  <option key={m.userId} value={m.userId}>
                    {m.userDisplayName || m.userEmail}
                  </option>
                ))}
              </select>
            </div>
          )}
          {action === 'ChangeStatus' && (
            <div className="min-w-[120px]">
              <label className="block text-xs text-gray-500 dark:text-slate-400 mb-1">Status</label>
              <select
                value={targetValue}
                onChange={(e) => setTargetValue(e.target.value)}
                className="w-full rounded-lg border border-gray-300 dark:border-slate-600 bg-white dark:bg-slate-800 text-gray-900 dark:text-slate-100 px-3 py-2 text-sm"
              >
                <option value="">Select</option>
                {STATUS_OPTIONS.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </select>
            </div>
          )}
          {action === 'AddLabel' && (
            <div className="min-w-[140px]">
              <label className="block text-xs text-gray-500 dark:text-slate-400 mb-1">Label</label>
              <select
                value={targetValue}
                onChange={(e) => setTargetValue(e.target.value)}
                className="w-full rounded-lg border border-gray-300 dark:border-slate-600 bg-white dark:bg-slate-800 text-gray-900 dark:text-slate-100 px-3 py-2 text-sm"
              >
                <option value="">Select label</option>
                {labels.map((l) => (
                  <option key={l.id} value={l.id}>
                    {l.name}
                  </option>
                ))}
              </select>
            </div>
          )}
          <button
            type="submit"
            disabled={saving}
            className="rounded-lg bg-blue-600 text-white px-4 py-2 text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
          >
            {saving ? 'Saving…' : 'Save'}
          </button>
        </form>
      </div>

      <div>
        <h3 className="text-sm font-medium text-gray-700 dark:text-slate-300 mb-2">Rules</h3>
        {rules.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-slate-400">No automation rules yet.</p>
        ) : (
          <ul className="divide-y divide-gray-200 dark:divide-slate-600 rounded-lg border border-gray-200 dark:border-slate-600 overflow-hidden">
            {rules.map((r) => (
              <li
                key={r.id}
                className="flex items-center justify-between gap-3 px-3 py-2 bg-white dark:bg-slate-800/50 text-sm"
              >
                <span className="text-gray-700 dark:text-slate-300">
                  When <strong>{triggerLabel(r.trigger)}</strong> → {actionLabel(r.action)}
                  {resolveTarget(r) && ` (${resolveTarget(r)})`}
                </span>
                <button
                  type="button"
                  onClick={() => handleDelete(r.id)}
                  disabled={deletingId === r.id}
                  className="text-red-600 hover:text-red-700 dark:text-red-400 text-xs font-medium disabled:opacity-50"
                >
                  {deletingId === r.id ? 'Removing…' : 'Remove'}
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
