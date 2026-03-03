import { useCallback, useEffect, useState } from 'react';
import { getProjectMembers, removeProjectMember, updateProjectMemberRole } from './memberService';
import { addToast } from '../../utils/toast';
import { Modal } from '../../components/Modal';
import type { ProjectMember, ProjectMemberRole } from './types';
import { PROJECT_MEMBER_ROLES } from './types';
import { AddMemberModal } from './AddMemberModal';
import { getCurrentUserId } from '../../services/authService';

function roleBadgeClass(role: ProjectMemberRole): string {
  if (role === 'Owner') return 'bg-purple-100 text-purple-800 dark:bg-purple-900/40 dark:text-purple-300';
  if (role === 'Member') return 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300';
  return 'bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300';
}

function getInitials(displayName: string, email: string): string {
  if (displayName?.trim()) {
    const parts = displayName.trim().split(/\s+/);
    if (parts.length >= 2) return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
    return displayName.slice(0, 2).toUpperCase();
  }
  if (email?.trim()) return email.slice(0, 2).toUpperCase();
  return '?';
}

interface ProjectMembersTabProps {
  projectId: string;
  canManageMembers: boolean;
  onMembersChange?: () => void;
}

export function ProjectMembersTab({ projectId, canManageMembers, onMembersChange }: ProjectMembersTabProps) {
  const [members, setMembers] = useState<ProjectMember[]>([]);
  const [loading, setLoading] = useState(true);
  const [addModalOpen, setAddModalOpen] = useState(false);
  const [removeTarget, setRemoveTarget] = useState<ProjectMember | null>(null);
  const [removing, setRemoving] = useState(false);
  const [updatingRoleId, setUpdatingRoleId] = useState<string | null>(null);
  const currentUserId = getCurrentUserId();

  const loadMembers = useCallback(() => {
    setLoading(true);
    getProjectMembers(projectId)
      .then((res) => {
        const data = res.data ?? (res as unknown as { Data?: ProjectMember[] }).Data;
        if (res.success && Array.isArray(data)) {
          setMembers(data);
        } else {
          addToast('error', res.error?.message ?? 'Failed to load members.');
        }
      })
      .finally(() => setLoading(false));
  }, [projectId]);

  useEffect(() => {
    loadMembers();
  }, [loadMembers]);

  const handleAddSaved = useCallback(() => {
    loadMembers();
    onMembersChange?.();
    setAddModalOpen(false);
  }, [loadMembers, onMembersChange]);

  const handleRoleChange = useCallback(
    async (memberId: string, newRole: ProjectMemberRole) => {
      setUpdatingRoleId(memberId);
      try {
        const res = await updateProjectMemberRole(projectId, memberId, { role: newRole });
        if (res.success && res.data) {
          setMembers((prev) => prev.map((m) => (m.id === memberId ? { ...m, role: newRole } : m)));
          onMembersChange?.();
          addToast('success', 'Role updated.');
        } else {
          addToast('error', res.error?.message ?? 'Failed to update role.');
        }
      } finally {
        setUpdatingRoleId(null);
      }
    },
    [projectId, onMembersChange]
  );

  const handleRemoveConfirm = useCallback(async () => {
    if (!removeTarget) return;
    setRemoving(true);
    try {
      const res = await removeProjectMember(projectId, removeTarget.id);
      if (res.success) {
        setMembers((prev) => prev.filter((m) => m.id !== removeTarget.id));
        onMembersChange?.();
        addToast('success', 'Member removed.');
        setRemoveTarget(null);
      } else {
        addToast('error', res.error?.message ?? 'Failed to remove member.');
      }
    } finally {
      setRemoving(false);
    }
  }, [projectId, removeTarget, onMembersChange]);

  const ownerCount = members.filter((m) => m.role === 'Owner').length;
  const isOnlyOwner = (m: ProjectMember) => m.role === 'Owner' && ownerCount <= 1;
  const isSelf = (m: ProjectMember) => currentUserId && m.userId === currentUserId;
  const canChangeRole = (m: ProjectMember) =>
    canManageMembers &&
    !isOnlyOwner(m) &&
    !(isSelf(m) && isOnlyOwner(m));
  const canRemove = (m: ProjectMember) =>
    canManageMembers && !isOnlyOwner(m) && (m.role !== 'Owner' || ownerCount > 1);

  if (loading) {
    return (
      <div className="animate-pulse space-y-3">
        <div className="h-6 w-48 bg-gray-200 dark:bg-slate-600 rounded" />
        <div className="h-14 bg-gray-100 dark:bg-slate-700 rounded-xl" />
        <div className="h-14 bg-gray-100 dark:bg-slate-700 rounded-xl" />
        <div className="h-14 bg-gray-100 dark:bg-slate-700 rounded-xl" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <h2 className="text-lg font-semibold" style={{ color: 'var(--card-header-color, #323130)' }}>
          Project Members
        </h2>
        {canManageMembers && (
          <button
            type="button"
            onClick={() => setAddModalOpen(true)}
            className="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-blue-600 text-white text-sm font-medium hover:bg-blue-700 transition-colors shadow-sm"
          >
            <span aria-hidden>+</span> Add Member
          </button>
        )}
      </div>

      <div className="rounded-xl border border-gray-100 dark:border-slate-700/50 overflow-hidden bg-white dark:bg-slate-800/50 shadow-sm">
        <ul className="divide-y divide-gray-100 dark:divide-slate-700/50">
          {members.length === 0 ? (
            <li className="px-4 py-8 text-center text-sm" style={{ color: 'var(--card-description-color, #605e5c)' }}>
              No members yet. Add members to collaborate.
            </li>
          ) : (
            members.map((member) => (
              <li
                key={member.id}
                className="flex flex-wrap items-center gap-4 px-4 py-3 hover:bg-gray-50 dark:hover:bg-slate-700/30 transition-colors"
              >
                <div
                  className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-slate-200 dark:bg-slate-600 text-sm font-medium text-slate-700 dark:text-slate-200"
                  aria-hidden
                >
                  {getInitials(member.userDisplayName ?? '', member.userEmail ?? '')}
                </div>
                <div className="min-w-0 flex-1">
                  <p className="font-medium truncate" style={{ color: 'var(--card-header-color, #323130)' }}>
                    {member.userDisplayName || member.userEmail || 'Unknown'}
                  </p>
                  {member.userEmail && (
                    <p className="text-sm truncate" style={{ color: 'var(--card-description-color, #605e5c)' }}>
                      {member.userEmail}
                    </p>
                  )}
                </div>
                <span
                  className={`inline-flex px-2.5 py-1 rounded-full text-xs font-medium shrink-0 ${roleBadgeClass(member.role)}`}
                >
                  {member.role}
                </span>
                {canManageMembers && (
                  <div className="flex items-center gap-2 shrink-0">
                    {canChangeRole(member) && (
                      <select
                        value={member.role}
                        onChange={(e) => handleRoleChange(member.id, e.target.value as ProjectMemberRole)}
                        disabled={updatingRoleId === member.id}
                        className="rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm py-1.5 px-2 text-gray-900 dark:text-slate-100 focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                      >
                        {PROJECT_MEMBER_ROLES.map((r) => (
                          <option key={r} value={r}>
                            {r}
                          </option>
                        ))}
                      </select>
                    )}
                    {canRemove(member) && (
                      <button
                        type="button"
                        onClick={() => setRemoveTarget(member)}
                        className="text-sm text-red-600 dark:text-red-400 hover:underline"
                      >
                        Remove
                      </button>
                    )}
                  </div>
                )}
              </li>
            ))
          )}
        </ul>
      </div>

      <AddMemberModal
        open={addModalOpen}
        onClose={() => setAddModalOpen(false)}
        onSaved={handleAddSaved}
        projectId={projectId}
        existingMemberIds={members.map((m) => m.userId)}
      />

      {removeTarget && (
        <Modal
          open={!!removeTarget}
          onClose={() => setRemoveTarget(null)}
          title="Remove member"
        >
          <p className="text-sm mb-4" style={{ color: 'var(--card-description-color, #605e5c)' }}>
            Remove {removeTarget.userDisplayName || removeTarget.userEmail} from this project? They will lose access.
          </p>
          <div className="flex justify-end gap-2">
            <button
              type="button"
              onClick={() => setRemoveTarget(null)}
              className="px-4 py-2 rounded-lg border border-gray-200 dark:border-slate-600 hover:bg-gray-50 dark:hover:bg-slate-700 transition-colors"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={handleRemoveConfirm}
              disabled={removing}
              className="px-4 py-2 rounded-lg bg-red-600 text-white hover:bg-red-700 disabled:opacity-50 transition-colors"
            >
              {removing ? 'Removing…' : 'Remove'}
            </button>
          </div>
        </Modal>
      )}
    </div>
  );
}
