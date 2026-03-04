import { useCallback, useEffect, useState } from 'react';
import {
  getTenantAdminUsers,
  inviteTenantUser,
  removeTenantUser,
  updateTenantUserRole,
  updateTenantUserModules,
  type TenantAdminUser,
  type InviteTenantUserRequest,
} from '../../services/tenantAdminService';
import { getTenantModulesManagement, type TenantModuleListItem } from '../../services/tenantModuleService';
import { addToast } from '../../utils/toast';
import { Modal } from '../../components/Modal';

const TENANT_ROLES = [
  { value: 'Admin', label: 'Admin' },
  { value: 'Member', label: 'Member' },
];

function AvatarPlaceholder({ name, className }: { name?: string; className?: string }) {
  const initial = name ? name.charAt(0).toUpperCase() : '?';
  return (
    <div
      className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300 text-sm font-medium ${className ?? ''}`}
      aria-hidden
    >
      {initial}
    </div>
  );
}

export function UserManagementSection() {
  const [users, setUsers] = useState<TenantAdminUser[]>([]);
  const [activeModules, setActiveModules] = useState<TenantModuleListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [inviteOpen, setInviteOpen] = useState(false);
  const [editUser, setEditUser] = useState<TenantAdminUser | null>(null);
  const [removeUser, setRemoveUser] = useState<TenantAdminUser | null>(null);
  const [saving, setSaving] = useState(false);
  const [rolePending, setRolePending] = useState<Record<string, string>>({});

  const loadUsers = useCallback(() => {
    setLoading(true);
    getTenantAdminUsers()
      .then((res) => {
        const data = res.data ?? (res as unknown as { data?: TenantAdminUser[] }).data;
        if (res.success && Array.isArray(data)) setUsers(data);
        else if (!res.success) addToast('error', res.error?.message ?? 'Failed to load users.');
      })
      .catch(() => addToast('error', 'Failed to load users.'))
      .finally(() => setLoading(false));
  }, []);

  const loadModules = useCallback(() => {
    getTenantModulesManagement()
      .then((res) => {
        const data = res.data ?? (res as unknown as { data?: TenantModuleListItem[] }).data;
        const list = Array.isArray(data) ? data : [];
        setActiveModules(list.filter((m) => m.isActive));
      })
      .catch(() => {});
  }, []);

  useEffect(() => {
    loadUsers();
    loadModules();
  }, [loadUsers, loadModules]);

  const handleInvite = (payload: InviteTenantUserRequest) => {
    setSaving(true);
    inviteTenantUser(payload)
      .then((res) => {
        if (res.success) {
          setInviteOpen(false);
          addToast('success', 'User added to tenant.');
          loadUsers();
        } else addToast('error', res.error?.message ?? 'Failed to add user.');
      })
      .finally(() => setSaving(false));
  };

  const handleRemove = (user: TenantAdminUser) => {
    setSaving(true);
    removeTenantUser(user.userId)
      .then((res) => {
        if (res.success) {
          setRemoveUser(null);
          addToast('success', 'User removed from tenant.');
          loadUsers();
        } else addToast('error', res.error?.message ?? 'Failed to remove user.');
      })
      .finally(() => setSaving(false));
  };

  const handleRoleChange = (user: TenantAdminUser, newRole: string) => {
    setRolePending((prev) => ({ ...prev, [user.userId]: newRole }));
    updateTenantUserRole(user.userId, newRole)
      .then((res) => {
        if (res.success) {
          setRolePending((prev) => {
            const next = { ...prev };
            delete next[user.userId];
            return next;
          });
          addToast('success', 'Role updated.');
          loadUsers();
        } else addToast('error', res.error?.message ?? 'Failed to update role.');
      });
  };

  const handleSaveEdit = (userId: string, role: string, moduleKeys: string[]) => {
    setSaving(true);
    Promise.all([
      updateTenantUserRole(userId, role),
      updateTenantUserModules(userId, moduleKeys),
    ])
      .then(([roleRes, modulesRes]) => {
        if (roleRes.success && modulesRes.success) {
          setEditUser(null);
          addToast('success', 'User updated.');
          loadUsers();
        } else addToast('error', roleRes.error?.message ?? modulesRes.error?.message ?? 'Failed to update.');
      })
      .finally(() => setSaving(false));
  };

  const adminCount = users.filter((u) => u.tenantRole === 'Admin').length;
  const isLastAdmin = (u: TenantAdminUser) => u.tenantRole === 'Admin' && adminCount <= 1;

  if (loading && users.length === 0) {
    return (
      <div className="space-y-4">
        <h2 className="text-lg font-semibold text-gray-900 dark:text-slate-100">User Management</h2>
        <div className="animate-pulse space-y-3">
          <div className="h-10 w-48 bg-gray-200 dark:bg-slate-600 rounded" />
          <div className="h-64 bg-gray-100 dark:bg-slate-700 rounded-xl" />
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <h2 className="text-lg font-semibold text-gray-900 dark:text-slate-100">User Management</h2>
        <button
          type="button"
          onClick={() => setInviteOpen(true)}
          className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:focus:ring-offset-gray-900 transition-colors"
        >
          <span aria-hidden>+</span>
          Invite User
        </button>
      </div>

      <div className="overflow-x-auto rounded-xl border border-gray-200 dark:border-[#2d3d5c] bg-white dark:bg-[#1e2a4a]">
        <table className="min-w-full divide-y divide-gray-200 dark:divide-[#2d3d5c]">
          <thead>
            <tr>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-slate-400 uppercase tracking-wider">User</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-slate-400 uppercase tracking-wider">Tenant Role</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-slate-400 uppercase tracking-wider">Modules</th>
              <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-slate-400 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 dark:divide-[#2d3d5c]">
            {users.map((u) => (
              <tr key={u.userId} className="transition-colors hover:bg-gray-50 dark:hover:bg-[#252f4a]">
                <td className="px-4 py-3">
                  <div className="flex items-center gap-3">
                    <AvatarPlaceholder name={u.displayName || u.email} />
                    <div>
                      <div className="font-medium text-gray-900 dark:text-slate-100">{u.displayName || u.email}</div>
                      <div className="text-sm text-gray-500 dark:text-slate-400">{u.email}</div>
                    </div>
                  </div>
                </td>
                <td className="px-4 py-3">
                  <span
                    className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                      (rolePending[u.userId] ?? u.tenantRole) === 'Admin'
                        ? 'bg-purple-100 text-purple-800 dark:bg-purple-900/40 dark:text-purple-300'
                        : 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300'
                    }`}
                  >
                    {rolePending[u.userId] ?? u.tenantRole}
                  </span>
                  <select
                    value={rolePending[u.userId] ?? u.tenantRole}
                    onChange={(e) => handleRoleChange(u, e.target.value)}
                    className="ml-2 rounded border border-gray-300 dark:border-slate-600 bg-white dark:bg-slate-800 text-gray-900 dark:text-slate-100 text-sm py-1 px-2 focus:ring-blue-500 focus:border-blue-500"
                    disabled={isLastAdmin(u)}
                    aria-label={`Change role for ${u.email}`}
                  >
                    {TENANT_ROLES.map((r) => (
                      <option key={r.value} value={r.value}>{r.label}</option>
                    ))}
                  </select>
                </td>
                <td className="px-4 py-3">
                  <div className="flex flex-wrap gap-1">
                    {u.activeModules.length === 0 ? (
                      <span className="text-sm text-gray-400 dark:text-slate-500">—</span>
                    ) : (
                      u.activeModules.map((key) => {
                        const mod = activeModules.find((m) => m.key === key);
                        return (
                          <span
                            key={key}
                            className="inline-flex rounded-md bg-gray-100 px-2 py-0.5 text-xs font-medium text-gray-700 dark:bg-slate-600 dark:text-slate-200"
                          >
                            {mod?.name ?? key}
                          </span>
                        );
                      })
                    )}
                  </div>
                </td>
                <td className="px-4 py-3 text-right">
                  <button
                    type="button"
                    onClick={() => setEditUser(u)}
                    className="text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 text-sm font-medium mr-3"
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    onClick={() => setRemoveUser(u)}
                    disabled={isLastAdmin(u)}
                    className="text-red-600 hover:text-red-800 dark:text-red-400 dark:hover:text-red-300 text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Remove
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {users.length === 0 && (
          <div className="px-6 py-12 text-center text-gray-500 dark:text-slate-400 text-sm">
            No users in this tenant yet. Use &quot;Invite User&quot; to add someone.
          </div>
        )}
      </div>

      {/* Invite modal */}
      <InviteModal
        open={inviteOpen}
        onClose={() => setInviteOpen(false)}
        activeModules={activeModules}
        onInvite={handleInvite}
        saving={saving}
      />

      {/* Edit drawer/modal */}
      {editUser && (
        <EditUserModal
          user={editUser}
          activeModules={activeModules}
          onClose={() => setEditUser(null)}
          onSave={handleSaveEdit}
          saving={saving}
          isLastAdmin={isLastAdmin(editUser)}
        />
      )}

      {/* Remove confirm */}
      <Modal
        open={!!removeUser}
        onClose={() => setRemoveUser(null)}
        title="Remove user from tenant"
      >
        {removeUser && (
          <div className="space-y-4">
            <p className="text-sm text-gray-600 dark:text-slate-400">
              Remove <strong>{removeUser.displayName || removeUser.email}</strong> from this tenant? They will lose access to all tenant data and modules.
            </p>
            <div className="flex justify-end gap-2">
              <button
                type="button"
                onClick={() => setRemoveUser(null)}
                className="rounded-lg border border-gray-300 dark:border-slate-600 px-4 py-2 text-sm font-medium text-gray-700 dark:text-slate-200 hover:bg-gray-50 dark:hover:bg-slate-800"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={() => handleRemove(removeUser)}
                disabled={saving}
                className="rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50"
              >
                {saving ? 'Removing…' : 'Remove'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}

interface InviteModalProps {
  open: boolean;
  onClose: () => void;
  activeModules: TenantModuleListItem[];
  onInvite: (req: InviteTenantUserRequest) => void;
  saving: boolean;
}

function InviteModal({ open, onClose, activeModules, onInvite, saving }: InviteModalProps) {
  const [email, setEmail] = useState('');
  const [tenantRole, setTenantRole] = useState('Member');
  const [moduleKeys, setModuleKeys] = useState<string[]>([]);

  useEffect(() => {
    if (open) {
      setEmail('');
      setTenantRole('Member');
      setModuleKeys([]);
    }
  }, [open]);

  const toggleModule = (key: string) => {
    setModuleKeys((prev) => (prev.includes(key) ? prev.filter((k) => k !== key) : [...prev, key]));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.trim()) return;
    onInvite({ email: email.trim(), tenantRole, moduleKeys });
  };

  if (!open) return null;
  return (
    <Modal open onClose={onClose} title="Invite User">
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Email</label>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            className="w-full rounded-lg border border-gray-300 dark:border-slate-600 bg-white dark:bg-slate-800 text-gray-900 dark:text-slate-100 px-3 py-2 text-sm focus:ring-blue-500 focus:border-blue-500"
            placeholder="user@example.com"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Tenant Role</label>
          <select
            value={tenantRole}
            onChange={(e) => setTenantRole(e.target.value)}
            className="w-full rounded-lg border border-gray-300 dark:border-slate-600 bg-white dark:bg-slate-800 text-gray-900 dark:text-slate-100 px-3 py-2 text-sm"
          >
            {TENANT_ROLES.map((r) => (
              <option key={r.value} value={r.value}>{r.label}</option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-2">Module Access</label>
          <div className="space-y-2 max-h-48 overflow-y-auto rounded border border-gray-200 dark:border-slate-600 p-3 bg-gray-50 dark:bg-slate-800/50">
            {activeModules.length === 0 ? (
              <p className="text-sm text-gray-500 dark:text-slate-400">No active modules for this tenant.</p>
            ) : (
              activeModules.map((m) => (
                <label key={m.key} className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={moduleKeys.includes(m.key)}
                    onChange={() => toggleModule(m.key)}
                    className="rounded border-gray-300 dark:border-slate-600 text-blue-600 focus:ring-blue-500"
                  />
                  <span className="text-sm text-gray-900 dark:text-slate-100">{m.name}</span>
                </label>
              ))
            )}
          </div>
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="rounded-lg border border-gray-300 dark:border-slate-600 px-4 py-2 text-sm font-medium text-gray-700 dark:text-slate-200 hover:bg-gray-50 dark:hover:bg-slate-800">
            Cancel
          </button>
          <button type="submit" disabled={saving || !email.trim()} className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50">
            {saving ? 'Adding…' : 'Add User'}
          </button>
        </div>
      </form>
    </Modal>
  );
}

interface EditUserModalProps {
  user: TenantAdminUser;
  activeModules: TenantModuleListItem[];
  onClose: () => void;
  onSave: (userId: string, role: string, moduleKeys: string[]) => void;
  saving: boolean;
  isLastAdmin: boolean;
}

function EditUserModal({ user, activeModules, onClose, onSave, saving, isLastAdmin }: EditUserModalProps) {
  const [role, setRole] = useState(user.tenantRole);
  const [moduleKeys, setModuleKeys] = useState<string[]>(user.activeModules ?? []);

  useEffect(() => {
    setRole(user.tenantRole);
    setModuleKeys(user.activeModules ?? []);
  }, [user]);

  const toggleModule = (key: string) => {
    setModuleKeys((prev) => (prev.includes(key) ? prev.filter((k) => k !== key) : [...prev, key]));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave(user.userId, role, moduleKeys);
  };

  return (
    <Modal open onClose={onClose} title={`Edit ${user.displayName || user.email}`}>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1">Tenant Role</label>
          <select
            value={role}
            onChange={(e) => setRole(e.target.value)}
            disabled={isLastAdmin}
            className="w-full rounded-lg border border-gray-300 dark:border-slate-600 bg-white dark:bg-slate-800 text-gray-900 dark:text-slate-100 px-3 py-2 text-sm"
          >
            {TENANT_ROLES.map((r) => (
              <option key={r.value} value={r.value}>{r.label}</option>
            ))}
          </select>
          {isLastAdmin && <p className="mt-1 text-xs text-amber-600 dark:text-amber-400">Cannot demote the last Admin.</p>}
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-2">Module Access</label>
          <div className="space-y-2 max-h-48 overflow-y-auto rounded border border-gray-200 dark:border-slate-600 p-3 bg-gray-50 dark:bg-slate-800/50">
            {activeModules.length === 0 ? (
              <p className="text-sm text-gray-500 dark:text-slate-400">No active modules.</p>
            ) : (
              activeModules.map((m) => (
                <label key={m.key} className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={moduleKeys.includes(m.key)}
                    onChange={() => toggleModule(m.key)}
                    className="rounded border-gray-300 dark:border-slate-600 text-blue-600 focus:ring-blue-500"
                  />
                  <span className="text-sm text-gray-900 dark:text-slate-100">{m.name}</span>
                </label>
              ))
            )}
          </div>
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="rounded-lg border border-gray-300 dark:border-slate-600 px-4 py-2 text-sm font-medium text-gray-700 dark:text-slate-200 hover:bg-gray-50 dark:hover:bg-slate-800">
            Cancel
          </button>
          <button type="submit" disabled={saving} className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50">
            {saving ? 'Saving…' : 'Save'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
