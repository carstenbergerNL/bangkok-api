import { useCallback, useEffect, useState } from 'react';
import { getUsers } from '../../services/userService';
import { addProjectMember } from './memberService';
import { addToast } from '../../utils/toast';
import { Modal } from '../../components/Modal';
import type { ProjectMemberRole } from './types';
import { PROJECT_MEMBER_ROLES } from './types';
import type { User } from '../../models/User';
import type { PagedResult } from '../../models/PagedResult';

interface AddMemberModalProps {
  open: boolean;
  onClose: () => void;
  onSaved: () => void;
  projectId: string;
  existingMemberIds: string[];
}

export function AddMemberModal({
  open,
  onClose,
  onSaved,
  projectId,
  existingMemberIds,
}: AddMemberModalProps) {
  const [users, setUsers] = useState<User[]>([]);
  const [search, setSearch] = useState('');
  const [selectedUserId, setSelectedUserId] = useState<string>('');
  const [role, setRole] = useState<ProjectMemberRole>('Member');
  const [saving, setSaving] = useState(false);
  const [loadingUsers, setLoadingUsers] = useState(false);

  const loadUsers = useCallback(() => {
    setLoadingUsers(true);
    getUsers(1, 500, false)
      .then((res) => {
        const data = res.data as PagedResult<User> | undefined;
        const items = data?.items ?? [];
        setUsers(Array.isArray(items) ? items : []);
      })
      .finally(() => setLoadingUsers(false));
  }, []);

  useEffect(() => {
    if (open) {
      loadUsers();
      setSearch('');
      setSelectedUserId('');
      setRole('Member');
    }
  }, [open, loadUsers]);

  const available = users.filter(
    (u) => !existingMemberIds.includes(u.id) && u.isActive !== false
  );
  const filtered =
    search.trim() === ''
      ? available
      : available.filter((u) => {
          const q = search.trim().toLowerCase();
          const name = (u.displayName ?? '').toLowerCase();
          const email = (u.email ?? '').toLowerCase();
          return name.includes(q) || email.includes(q);
        });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedUserId) {
      addToast('error', 'Please select a user.');
      return;
    }
    setSaving(true);
    try {
      const res = await addProjectMember(projectId, { userId: selectedUserId, role });
      if (res.success) {
        onSaved();
      } else {
        addToast('error', res.error?.message ?? 'Failed to add member.');
      }
    } finally {
      setSaving(false);
    }
  };

  if (!open) return null;

  return (
    <Modal open={open} onClose={onClose} title="Add member">
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label htmlFor="add-member-user" className="block text-sm font-medium mb-1" style={{ color: 'var(--card-header-color, #323130)' }}>
            User
          </label>
          <input
            id="add-member-search"
            type="text"
            placeholder="Search by name or email..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 mb-2"
          />
          <select
            id="add-member-user"
            required
            value={selectedUserId}
            onChange={(e) => setSelectedUserId(e.target.value)}
            className="w-full rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          >
            <option value="">Select user…</option>
            {filtered.map((u) => (
              <option key={u.id} value={u.id}>
                {u.displayName || u.email || u.id} {u.email ? `(${u.email})` : ''}
              </option>
            ))}
            {available.length > 0 && filtered.length === 0 && (
              <option value="" disabled>No matches</option>
            )}
            {available.length === 0 && (
              <option value="" disabled>No users available to add</option>
            )}
          </select>
          {loadingUsers && <p className="text-xs mt-1 text-gray-500">Loading users…</p>}
        </div>
        <div>
          <label htmlFor="add-member-role" className="block text-sm font-medium mb-1" style={{ color: 'var(--card-header-color, #323130)' }}>
            Role
          </label>
          <select
            id="add-member-role"
            value={role}
            onChange={(e) => setRole(e.target.value as ProjectMemberRole)}
            className="w-full rounded-lg border border-gray-200 dark:border-slate-600 bg-white dark:bg-slate-800 px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          >
            {PROJECT_MEMBER_ROLES.map((r) => (
              <option key={r} value={r}>
                {r}
              </option>
            ))}
          </select>
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 rounded-lg border border-gray-200 dark:border-slate-600 hover:bg-gray-50 dark:hover:bg-slate-700 transition-colors"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={saving || !selectedUserId}
            className="px-4 py-2 rounded-lg bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50 transition-colors"
          >
            {saving ? 'Adding…' : 'Add'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
