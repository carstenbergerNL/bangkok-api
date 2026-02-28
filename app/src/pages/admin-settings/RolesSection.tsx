import { useCallback, useEffect, useMemo, useState } from 'react';
import { getUsers } from '../../services/userService';
import {
  getRoles,
  createRole,
  updateRole,
  deleteRole,
} from '../../services/roleService';
import {
  getPermissions,
  getPermissionsForRole,
  assignPermissionToRole,
  removePermissionFromRole,
} from '../../services/permissionService';
import { addToast } from '../../utils/toast';
import type { Role } from '../../models/Role';
import type { Permission } from '../../models/Permission';
import type { User } from '../../models/User';
import { Modal } from '../../components/Modal';
import { TableSkeleton } from '../../components/TableSkeleton';

type RoleModal = 'create' | 'edit' | 'delete' | null;

export function RolesSection() {
  const [roles, setRoles] = useState<Role[]>([]);
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [modal, setModal] = useState<RoleModal>(null);
  const [selectedRole, setSelectedRole] = useState<Role | null>(null);

  const loadRoles = useCallback(() => {
    setLoading(true);
    setError(null);
    getRoles()
      .then((res) => {
        if (res.success && res.data) setRoles(res.data);
        else setError(res.error?.message ?? res.message ?? 'Failed to load roles.');
      })
      .catch(() => {
        setError('Network or server error.');
        addToast('error', 'Failed to load roles.');
      })
      .finally(() => setLoading(false));
  }, []);

  const loadUsers = useCallback(() => {
    getUsers(1, 500, false).then((res) => {
      if (res.success && res.data?.items) {
        setUsers(res.data.items.filter((u) => !u.isDeleted));
      }
    });
  }, []);

  useEffect(() => {
    loadRoles();
    loadUsers();
  }, [loadRoles, loadUsers]);

  const userCountByRole = useMemo(() => {
    const map: Record<string, number> = {};
    roles.forEach((r) => { map[r.name] = 0; });
    users.forEach((u) => {
      (u.roles ?? []).forEach((r) => {
        if (map[r] !== undefined) map[r]++;
      });
    });
    return map;
  }, [roles, users]);

  const openEdit = (r: Role) => {
    setSelectedRole(r);
    setModal('edit');
  };
  const openDelete = (r: Role) => {
    setSelectedRole(r);
    setModal('delete');
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <p className="text-sm text-gray-600 dark:text-gray-400">Create and manage roles. Assign permissions in Edit.</p>
        <button type="button" onClick={() => { setSelectedRole(null); setModal('create'); }} className="btn-primary shrink-0">
          + Create Role
        </button>
      </div>

      <div className="rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden bg-gray-50/50 dark:bg-gray-800/30">
        {error && !loading && <div className="p-4"><div className="alert-error">{error}</div></div>}
        {loading && <div className="p-6"><TableSkeleton rows={6} cols={4} /></div>}
        {!loading && !error && roles.length === 0 && (
          <div className="p-12 text-center text-gray-500 dark:text-gray-400 text-sm">No roles yet. Create one to get started.</div>
        )}
        {!loading && !error && roles.length > 0 && (
          <div className="overflow-x-auto">
            <table className="table-grid">
              <thead>
                <tr>
                  <th>Role Name</th>
                  <th>Description</th>
                  <th>Users</th>
                  <th className="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {roles.map((r) => (
                  <tr key={r.id}>
                    <td className="font-medium text-gray-900 dark:text-white">{r.name}</td>
                    <td className="muted">{r.description ?? '—'}</td>
                    <td>{userCountByRole[r.name] ?? 0}</td>
                    <td className="text-right">
                      <div className="table-actions">
                        <button type="button" onClick={() => openEdit(r)} className="table-link">Edit</button>
                        <button type="button" onClick={() => openDelete(r)} className="table-link-danger">Delete</button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {modal === 'create' && (
        <CreateRoleModal
          onClose={() => { setModal(null); setSelectedRole(null); }}
          onSuccess={() => { setModal(null); setSelectedRole(null); loadRoles(); addToast('success', 'Role created.'); }}
        />
      )}
      {modal === 'edit' && selectedRole && (
        <EditRoleModal
          role={selectedRole}
          onClose={() => { setModal(null); setSelectedRole(null); }}
          onSuccess={() => { setModal(null); setSelectedRole(null); loadRoles(); addToast('success', 'Role updated.'); }}
        />
      )}
      {modal === 'delete' && selectedRole && (
        <DeleteRoleModal
          role={selectedRole}
          userCount={userCountByRole[selectedRole.name] ?? 0}
          onClose={() => { setModal(null); setSelectedRole(null); }}
          onSuccess={() => { setModal(null); setSelectedRole(null); loadRoles(); addToast('success', 'Role deleted.'); }}
        />
      )}
    </div>
  );
}

function CreateRoleModal({ onClose, onSuccess }: { onClose: () => void; onSuccess: () => void }) {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    if (!name.trim()) { setFormError('Name is required.'); return; }
    setSubmitting(true);
    createRole({ name: name.trim(), description: description.trim() || undefined })
      .then((res) => {
        if (res.success) onSuccess();
        else setFormError(res.error?.message ?? res.message ?? 'Failed to create role.');
      })
      .catch(() => setFormError('Network or server error.'))
      .finally(() => setSubmitting(false));
  };

  return (
    <Modal open={true} title="Create Role" onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-4">
        {formError && <div className="alert-error">{formError}</div>}
        <div className="form-group">
          <label htmlFor="cr-name" className="input-label">Role name *</label>
          <input id="cr-name" type="text" value={name} onChange={(e) => setName(e.target.value)} className="input" required maxLength={100} />
        </div>
        <div className="form-group">
          <label htmlFor="cr-desc" className="input-label">Description</label>
          <input id="cr-desc" type="text" value={description} onChange={(e) => setDescription(e.target.value)} className="input" maxLength={255} />
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="submit" disabled={submitting} className="btn-primary">{submitting ? 'Creating…' : 'Create'}</button>
        </div>
      </form>
    </Modal>
  );
}

function EditRoleModal({ role, onClose, onSuccess }: { role: Role; onClose: () => void; onSuccess: () => void }) {
  const [name, setName] = useState(role.name);
  const [description, setDescription] = useState(role.description ?? '');
  const [permissions, setPermissions] = useState<Permission[]>([]);
  const [initialRolePermissionIds, setInitialRolePermissionIds] = useState<Set<string>>(new Set());
  const [rolePermissions, setRolePermissions] = useState<Set<string>>(new Set());
  const [loadingPerms, setLoadingPerms] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([getPermissions(), getPermissionsForRole(role.id)])
      .then(([allRes, roleRes]) => {
        if (allRes.success && allRes.data) setPermissions(allRes.data);
        if (roleRes.success && roleRes.data) {
          const ids = new Set(roleRes.data.map((p) => p.id));
          setInitialRolePermissionIds(ids);
          setRolePermissions(ids);
        }
      })
      .finally(() => setLoadingPerms(false));
  }, [role.id]);

  const togglePermission = (permId: string) => {
    setRolePermissions((prev) => {
      const next = new Set(prev);
      if (next.has(permId)) next.delete(permId);
      else next.add(permId);
      return next;
    });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    if (!name.trim()) { setFormError('Name is required.'); return; }
    setSubmitting(true);
    updateRole(role.id, { name: name.trim(), description: description.trim() || undefined })
      .then((res) => {
        if (!res.success) {
          setFormError(res.error?.message ?? res.message ?? 'Failed to update role.');
          setSubmitting(false);
          return;
        }
        const current = initialRolePermissionIds;
        const desired = rolePermissions;
        const toAdd = permissions.filter((p) => desired.has(p.id) && !current.has(p.id));
        const toRemove = permissions.filter((p) => current.has(p.id) && !desired.has(p.id));
        return Promise.all([
          ...toAdd.map((p) => assignPermissionToRole(role.id, p.id)),
          ...toRemove.map((p) => removePermissionFromRole(role.id, p.id)),
        ]);
      })
      .then(() => onSuccess())
      .catch(() => setFormError('Failed to update role or permissions.'))
      .finally(() => setSubmitting(false));
  };

  return (
    <Modal open={true} title="Edit Role" onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-4">
        {formError && <div className="alert-error">{formError}</div>}
        <div className="form-group">
          <label htmlFor="er-name" className="input-label">Role name *</label>
          <input id="er-name" type="text" value={name} onChange={(e) => setName(e.target.value)} className="input" required maxLength={100} />
        </div>
        <div className="form-group">
          <label htmlFor="er-desc" className="input-label">Description</label>
          <input id="er-desc" type="text" value={description} onChange={(e) => setDescription(e.target.value)} className="input" maxLength={255} />
        </div>
        <div className="form-group">
          <label className="input-label">Permissions</label>
          {loadingPerms ? (
            <p className="text-sm text-gray-500 dark:text-gray-400">Loading…</p>
          ) : (
            <div className="space-y-2 max-h-48 overflow-y-auto rounded border border-gray-200 dark:border-gray-700 p-3">
              {permissions.length === 0 ? (
                <p className="text-sm text-gray-500 dark:text-gray-400">No permissions defined.</p>
              ) : (
                permissions.map((p) => (
                  <label key={p.id} className="flex items-center gap-2 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800/50 rounded px-2 py-1">
                    <input
                      type="checkbox"
                      checked={rolePermissions.has(p.id)}
                      onChange={() => togglePermission(p.id)}
                      className="rounded border-gray-300 dark:border-gray-600 text-blue-600 focus:ring-blue-500"
                    />
                    <span className="text-sm font-medium text-gray-900 dark:text-white">{p.name}</span>
                    {p.description && <span className="text-xs text-gray-500 dark:text-gray-400">— {p.description}</span>}
                  </label>
                ))
              )}
            </div>
          )}
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="submit" disabled={submitting || loadingPerms} className="btn-primary">{submitting ? 'Saving…' : 'Save'}</button>
        </div>
      </form>
    </Modal>
  );
}

function DeleteRoleModal({
  role,
  userCount,
  onClose,
  onSuccess,
}: { role: Role; userCount: number; onClose: () => void; onSuccess: () => void }) {
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const canDelete = userCount === 0;

  const handleConfirm = () => {
    if (!canDelete) return;
    setFormError(null);
    setSubmitting(true);
    deleteRole(role.id)
      .then((res) => {
        if (res.success) onSuccess();
        else setFormError(res.error?.message ?? res.message ?? 'Failed to delete role.');
      })
      .catch(() => setFormError('Network or server error.'))
      .finally(() => setSubmitting(false));
  };

  return (
    <Modal open={true} title="Delete role" onClose={onClose}>
      <div className="space-y-4">
        {formError && <div className="alert-error">{formError}</div>}
        <p className="text-gray-600 dark:text-gray-400">
          Delete role <strong className="text-gray-900 dark:text-white">{role.name}</strong>? This will remove it from all users.
        </p>
        {userCount > 0 && (
          <p className="text-sm text-amber-600 dark:text-amber-400">
            This role is assigned to {userCount} user(s). Remove it from all users first, then delete the role.
          </p>
        )}
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button
            type="button"
            onClick={handleConfirm}
            disabled={submitting || !canDelete}
            className="btn-danger disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {submitting ? 'Deleting…' : 'Delete'}
          </button>
        </div>
      </div>
    </Modal>
  );
}
