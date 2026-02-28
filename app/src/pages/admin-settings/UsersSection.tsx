import { useCallback, useEffect, useMemo, useState } from 'react';
import { useAuth } from '../../context/AuthContext';
import {
  createUser,
  deleteUser,
  getUsers,
  updateUser,
  type UpdateUserRequest,
} from '../../services/userService';
import { getRoles, assignRoleToUser, removeRoleFromUser } from '../../services/roleService';
import { addToast } from '../../utils/toast';
import type { User } from '../../models/User';
import type { Role } from '../../models/Role';
import { Modal } from '../../components/Modal';
import { TableSkeleton } from '../../components/TableSkeleton';

type UserModal = 'create' | 'edit' | 'assign-role' | 'disable' | null;

function avatarInitials(name?: string | null, email?: string): string {
  if (name?.trim()) {
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    return name.slice(0, 2).toUpperCase();
  }
  if (email) return email.slice(0, 2).toUpperCase();
  return '?';
}

export function UsersSection() {
  const { user: currentUser } = useAuth();
  const [users, setUsers] = useState<User[]>([]);
  const [allRoles, setAllRoles] = useState<Role[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [modal, setModal] = useState<UserModal>(null);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);

  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'disabled'>('all');

  const loadUsers = useCallback(() => {
    setLoading(true);
    setError(null);
    getUsers(1, 500, false)
      .then((res) => {
        if (res.success && res.data?.items) {
          setUsers(res.data.items.filter((u) => !u.isDeleted));
        } else {
          setError(res.error?.message ?? res.message ?? 'Failed to load users.');
        }
      })
      .catch(() => {
        setError('Network or server error.');
        addToast('error', 'Failed to load users.');
      })
      .finally(() => setLoading(false));
  }, []);

  const loadRoles = useCallback(() => {
    getRoles().then((res) => {
      if (res.success && res.data) setAllRoles(res.data);
    });
  }, []);

  useEffect(() => {
    loadUsers();
    loadRoles();
  }, [loadUsers, loadRoles]);

  const filteredUsers = useMemo(() => {
    return users.filter((u) => {
      const matchSearch =
        !search.trim() ||
        u.email?.toLowerCase().includes(search.toLowerCase()) ||
        u.displayName?.toLowerCase().includes(search.toLowerCase());
      const matchRole =
        !roleFilter ||
        (u.roles ?? []).some((r) => r?.toLowerCase() === roleFilter.toLowerCase());
      const matchStatus =
        statusFilter === 'all' ||
        (statusFilter === 'active' && u.isActive) ||
        (statusFilter === 'disabled' && !u.isActive);
      return matchSearch && matchRole && matchStatus;
    });
  }, [users, search, roleFilter, statusFilter]);

  const roleOptions = useMemo(() => {
    const names = new Set<string>();
    users.forEach((u) => (u.roles ?? []).forEach((r) => names.add(r)));
    return Array.from(names).sort();
  }, [users]);

  const openEdit = (u: User) => {
    setSelectedUser(u);
    setModal('edit');
  };
  const openAssignRole = (u: User) => {
    setSelectedUser(u);
    setModal('assign-role');
  };
  const openDisable = (u: User) => {
    setSelectedUser(u);
    setModal('disable');
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div className="flex flex-col sm:flex-row sm:items-center gap-3 flex-wrap">
          <input
            type="search"
            placeholder="Search by email or name…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="input max-w-xs"
          />
          <select
            value={roleFilter}
            onChange={(e) => setRoleFilter(e.target.value)}
            className="input w-auto min-w-[120px]"
          >
            <option value="">All roles</option>
            {roleOptions.map((r) => (
              <option key={r} value={r}>{r}</option>
            ))}
          </select>
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as typeof statusFilter)}
            className="input w-auto min-w-[120px]"
          >
            <option value="all">All status</option>
            <option value="active">Active</option>
            <option value="disabled">Disabled</option>
          </select>
        </div>
        <button type="button" onClick={() => { setSelectedUser(null); setModal('create'); }} className="btn-primary shrink-0">
          + Create User
        </button>
      </div>

      <div className="rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden bg-gray-50/50 dark:bg-gray-800/30">
        {error && !loading && <div className="p-4"><div className="alert-error">{error}</div></div>}
        {loading && <div className="p-6"><TableSkeleton rows={8} cols={6} /></div>}
        {!loading && !error && filteredUsers.length === 0 && (
          <div className="p-12 text-center text-gray-500 dark:text-gray-400 text-sm">No users match your filters.</div>
        )}
        {!loading && !error && filteredUsers.length > 0 && (
          <div className="overflow-x-auto">
            <table className="table-grid">
              <thead>
                <tr>
                  <th className="w-12">Avatar</th>
                  <th>Display Name</th>
                  <th>Email</th>
                  <th>Roles</th>
                  <th>Status</th>
                  <th className="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredUsers.map((u) => {
                  const isSelf = currentUser?.email != null && u.email != null &&
                    currentUser.email.trim().toLowerCase() === u.email.trim().toLowerCase();
                  return (
                    <tr key={u.id}>
                      <td>
                        <div
                          className="w-9 h-9 rounded-full flex items-center justify-center text-sm font-medium bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300"
                          title={u.displayName ?? u.email}
                        >
                          {avatarInitials(u.displayName, u.email)}
                        </div>
                      </td>
                      <td className="font-medium text-gray-900 dark:text-white">{u.displayName ?? '—'}</td>
                      <td className="muted">{u.email}</td>
                      <td>
                        <div className="flex flex-wrap gap-1">
                          {(u.roles ?? []).length === 0 ? (
                            <span className="text-gray-400 dark:text-gray-500 text-xs">—</span>
                          ) : (
                            (u.roles ?? []).map((r) => (
                              <span
                                key={r}
                                className="inline-flex px-2 py-0.5 text-xs font-medium rounded-full bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300"
                              >
                                {r}
                              </span>
                            ))
                          )}
                        </div>
                      </td>
                      <td>
                        <span className={u.isActive ? 'text-emerald-600 dark:text-emerald-400 font-medium' : 'text-gray-400 dark:text-gray-500'}>
                          {u.isActive ? 'Active' : 'Disabled'}
                        </span>
                      </td>
                      <td className="text-right">
                        <div className="table-actions">
                          <button type="button" onClick={() => openEdit(u)} className="table-link">Edit</button>
                          <button type="button" onClick={() => openAssignRole(u)} className="table-link">Assign Role</button>
                          {!isSelf && (
                            <button type="button" onClick={() => openDisable(u)} className="table-link-danger">
                              {u.isActive ? 'Disable' : 'Enable'}
                            </button>
                          )}
                          {isSelf && <span className="text-gray-400 dark:text-gray-500 text-xs">(you)</span>}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {modal === 'create' && (
        <CreateUserModal
          roles={allRoles}
          onClose={() => { setModal(null); setSelectedUser(null); }}
          onSuccess={() => { setModal(null); setSelectedUser(null); loadUsers(); addToast('success', 'User created.'); }}
        />
      )}
      {modal === 'edit' && selectedUser && (
        <EditUserModal
          user={selectedUser}
          roles={allRoles}
          onClose={() => { setModal(null); setSelectedUser(null); }}
          onSuccess={() => { setModal(null); setSelectedUser(null); loadUsers(); addToast('success', 'User updated.'); }}
        />
      )}
      {modal === 'assign-role' && selectedUser && (
        <AssignRoleModal
          user={selectedUser}
          roles={allRoles}
          onClose={() => { setModal(null); setSelectedUser(null); }}
          onSuccess={() => { setModal(null); setSelectedUser(null); loadUsers(); addToast('success', 'Roles updated.'); }}
        />
      )}
      {modal === 'disable' && selectedUser && (
        <DisableUserModal
          user={selectedUser}
          onClose={() => { setModal(null); setSelectedUser(null); }}
          onSuccess={() => { setModal(null); setSelectedUser(null); loadUsers(); addToast('success', selectedUser.isActive ? 'User disabled.' : 'User enabled.'); }}
        />
      )}
    </div>
  );
}

function CreateUserModal({
  roles,
  onClose,
  onSuccess,
}: { roles: Role[]; onClose: () => void; onSuccess: () => void }) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [role, setRole] = useState(roles[0]?.name ?? 'User');
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    if (!email.trim()) { setFormError('Email is required.'); return; }
    if (!password || password.length < 8) { setFormError('Password must be at least 8 characters.'); return; }
    setSubmitting(true);
    createUser({ email: email.trim(), password, displayName: displayName.trim() || undefined, role })
      .then((res) => {
        if (res.success) onSuccess();
        else setFormError(res.error?.message ?? res.message ?? 'Failed to create user.');
      })
      .catch(() => setFormError('Network or server error.'))
      .finally(() => setSubmitting(false));
  };

  return (
    <Modal open={true} title="Create User" onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-4">
        {formError && <div className="alert-error">{formError}</div>}
        <div className="form-group">
          <label htmlFor="cu-email" className="input-label">Email *</label>
          <input id="cu-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} className="input" required autoComplete="email" />
        </div>
        <div className="form-group">
          <label htmlFor="cu-password" className="input-label">Password * (min 8 characters)</label>
          <input id="cu-password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} className="input" required minLength={8} autoComplete="new-password" />
        </div>
        <div className="form-group">
          <label htmlFor="cu-displayName" className="input-label">Display name</label>
          <input id="cu-displayName" type="text" value={displayName} onChange={(e) => setDisplayName(e.target.value)} className="input" maxLength={256} />
        </div>
        <div className="form-group">
          <label htmlFor="cu-role" className="input-label">Initial role</label>
          <select id="cu-role" value={role} onChange={(e) => setRole(e.target.value)} className="input">
            {roles.length === 0 ? <option value="User">User</option> : roles.map((r) => <option key={r.id} value={r.name}>{r.name}</option>)}
          </select>
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="submit" disabled={submitting} className="btn-primary">{submitting ? 'Creating…' : 'Create'}</button>
        </div>
      </form>
    </Modal>
  );
}

function EditUserModal({
  user,
  roles,
  onClose,
  onSuccess,
}: { user: User; roles: Role[]; onClose: () => void; onSuccess: () => void }) {
  const [email, setEmail] = useState(user.email);
  const [displayName, setDisplayName] = useState(user.displayName ?? '');
  const [role, setRole] = useState(user.roles?.[0] ?? 'User');
  const [isActive, setIsActive] = useState(user.isActive);
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const { user: currentUser } = useAuth();
  const isSelf = currentUser?.email != null && user.email != null && currentUser.email.trim().toLowerCase() === user.email.trim().toLowerCase();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    const payload: UpdateUserRequest = {
      email: email.trim() || undefined,
      displayName: displayName.trim() || undefined,
      role,
      ...(isSelf ? {} : { isActive }),
    };
    setSubmitting(true);
    updateUser(user.id, payload)
      .then((res) => {
        if (res.success) onSuccess();
        else setFormError(res.error?.message ?? res.message ?? 'Failed to update user.');
      })
      .catch(() => setFormError('Network or server error.'))
      .finally(() => setSubmitting(false));
  };

  return (
    <Modal open={true} title="Edit User" onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-4">
        {formError && <div className="alert-error">{formError}</div>}
        <div className="form-group">
          <label htmlFor="eu-email" className="input-label">Email</label>
          <input id="eu-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} className="input" />
        </div>
        <div className="form-group">
          <label htmlFor="eu-displayName" className="input-label">Display name</label>
          <input id="eu-displayName" type="text" value={displayName} onChange={(e) => setDisplayName(e.target.value)} className="input" maxLength={256} />
        </div>
        <div className="form-group">
          <label htmlFor="eu-role" className="input-label">Primary role</label>
          <select id="eu-role" value={role} onChange={(e) => setRole(e.target.value)} className="input">
            {roles.length === 0 ? <option value="User">User</option> : roles.map((r) => <option key={r.id} value={r.name}>{r.name}</option>)}
          </select>
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">For multiple roles, use Assign Role from the table.</p>
        </div>
        {!isSelf && (
          <div className="flex items-center gap-2">
            <input
              id="eu-isActive"
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              className="rounded border-gray-300 dark:border-gray-600 text-blue-600 focus:ring-blue-500"
            />
            <label htmlFor="eu-isActive" className="text-sm font-medium text-gray-700 dark:text-gray-300">Active</label>
          </div>
        )}
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="submit" disabled={submitting} className="btn-primary">{submitting ? 'Saving…' : 'Save'}</button>
        </div>
      </form>
    </Modal>
  );
}

function AssignRoleModal({
  user,
  roles,
  onClose,
  onSuccess,
}: { user: User; roles: Role[]; onClose: () => void; onSuccess: () => void }) {
  const currentRoleNames = (user.roles ?? []).slice();
  const [selectedIds, setSelectedIds] = useState<Set<string>>(() => {
    const ids = new Set<string>();
    roles.forEach((r) => {
      if (currentRoleNames.some((n) => n?.toLowerCase() === r.name.toLowerCase())) ids.add(r.id);
    });
    return ids;
  });
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const toggle = (roleId: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(roleId)) next.delete(roleId);
      else next.add(roleId);
      return next;
    });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    if (selectedIds.size === 0) { setFormError('Select at least one role.'); return; }
    setSubmitting(true);
    const toRemove = currentRoleNames
      .map((name) => roles.find((r) => r.name.toLowerCase() === name?.toLowerCase())?.id)
      .filter((id): id is string => !!id && !selectedIds.has(id));
    const toAdd = Array.from(selectedIds).filter(
      (id) => !currentRoleNames.some((n) => roles.find((r) => r.id === id)?.name.toLowerCase() === n?.toLowerCase())
    );
    Promise.all([
      ...toRemove.map((roleId) => removeRoleFromUser(user.id, roleId)),
      ...toAdd.map((roleId) => assignRoleToUser(user.id, roleId)),
    ])
      .then(() => onSuccess())
      .catch((err: { response?: { data?: { error?: { message?: string }; message?: string }; status?: number }; message?: string }) => {
        const msg = err.response?.data?.error?.message ?? err.response?.data?.message ?? err.message ?? 'Failed to update roles.';
        setFormError(msg);
      })
      .finally(() => setSubmitting(false));
  };

  return (
    <Modal open={true} title={`Assign roles: ${user.email}`} onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-4">
        {formError && <div className="alert-error">{formError}</div>}
        <p className="text-sm text-gray-600 dark:text-gray-400">Select all roles this user should have.</p>
        <div className="space-y-2 max-h-64 overflow-y-auto py-1">
          {roles.map((r) => (
            <label key={r.id} className="flex items-center gap-2 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800/50 rounded px-2 py-1.5">
              <input
                type="checkbox"
                checked={selectedIds.has(r.id)}
                onChange={() => toggle(r.id)}
                className="rounded border-gray-300 dark:border-gray-600 text-blue-600 focus:ring-blue-500"
              />
              <span className="text-sm font-medium text-gray-900 dark:text-white">{r.name}</span>
              {r.description && <span className="text-xs text-gray-500 dark:text-gray-400">— {r.description}</span>}
            </label>
          ))}
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="submit" disabled={submitting} className="btn-primary">{submitting ? 'Saving…' : 'Save'}</button>
        </div>
      </form>
    </Modal>
  );
}

function DisableUserModal({ user, onClose, onSuccess }: { user: User; onClose: () => void; onSuccess: () => void }) {
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const enabling = !user.isActive;

  const handleConfirm = () => {
    setFormError(null);
    setSubmitting(true);
    updateUser(user.id, { isActive: enabling })
      .then((res) => {
        if (res.success) onSuccess();
        else setFormError(res.error?.message ?? res.message ?? 'Failed to update user.');
      })
      .catch(() => setFormError('Network or server error.'))
      .finally(() => setSubmitting(false));
  };

  return (
    <Modal open={true} title={enabling ? 'Enable user' : 'Disable user'} onClose={onClose}>
      <div className="space-y-4">
        {formError && <div className="alert-error">{formError}</div>}
        <p className="text-gray-600 dark:text-gray-400">
          {enabling
            ? <>Enable <strong className="text-gray-900 dark:text-white">{user.email}</strong>? They will be able to sign in again.</>
            : <>Disable <strong className="text-gray-900 dark:text-white">{user.email}</strong>? They will not be able to sign in until enabled.</>
          }
        </p>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="button" onClick={handleConfirm} disabled={submitting} className={enabling ? 'btn-primary' : 'btn-danger'}>
            {submitting ? 'Updating…' : enabling ? 'Enable' : 'Disable'}
          </button>
        </div>
      </div>
    </Modal>
  );
}
