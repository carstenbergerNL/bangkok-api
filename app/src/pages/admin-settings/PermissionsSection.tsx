import { useCallback, useEffect, useMemo, useState } from 'react';
import { getRoles } from '../../services/roleService';
import { getPermissions, getPermissionsForRole, createPermission, updatePermission, deletePermission } from '../../services/permissionService';
import { addToast } from '../../utils/toast';
import type { Permission } from '../../models/Permission';
import type { Role } from '../../models/Role';
import { Modal } from '../../components/Modal';
import { TableSkeleton } from '../../components/TableSkeleton';

export function PermissionsSection() {
  const [permissions, setPermissions] = useState<Permission[]>([]);
  const [roles, setRoles] = useState<Role[]>([]);
  const [rolePermissionsMap, setRolePermissionsMap] = useState<Record<string, string[]>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [editPermission, setEditPermission] = useState<Permission | null>(null);
  const [deletePermissionState, setDeletePermissionState] = useState<Permission | null>(null);

  const loadPermissions = useCallback(() => {
    setLoading(true);
    setError(null);
    getPermissions()
      .then((res) => {
        if (res.success && res.data) setPermissions(res.data);
        else setError(res.error?.message ?? res.message ?? 'Failed to load permissions.');
      })
      .catch(() => {
        setError('Network or server error.');
        addToast('error', 'Failed to load permissions.');
      })
      .finally(() => setLoading(false));
  }, []);

  const loadRoles = useCallback(() => {
    getRoles().then((res) => {
      if (res.success && res.data) setRoles(res.data);
    });
  }, []);

  useEffect(() => {
    loadPermissions();
    loadRoles();
  }, [loadPermissions, loadRoles]);

  useEffect(() => {
    if (roles.length === 0) {
      setRolePermissionsMap({});
      return;
    }
    const map: Record<string, string[]> = {};
    permissions.forEach((p) => { map[p.id] = []; });
    Promise.all(roles.map((r) => getPermissionsForRole(r.id)))
      .then((results) => {
        results.forEach((res, i) => {
          const role = roles[i];
          if (!role || !res.success || !res.data) return;
          res.data.forEach((p) => {
            if (!map[p.id]) map[p.id] = [];
            map[p.id].push(role.name);
          });
        });
        setRolePermissionsMap(map);
      });
  }, [roles, permissions]);

  const permissionToRoles = useMemo(() => {
    return rolePermissionsMap;
  }, [rolePermissionsMap]);

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <p className="text-sm text-gray-600 dark:text-gray-400">Create, edit, and delete permissions. Assign to roles via Roles → Edit role.</p>
        <button type="button" onClick={() => setCreateOpen(true)} className="btn-primary shrink-0">
          + Add Permission
        </button>
      </div>

      <div className="rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden bg-gray-50/50 dark:bg-gray-800/30">
        {error && !loading && <div className="p-4"><div className="alert-error">{error}</div></div>}
        {loading && <div className="p-6"><TableSkeleton rows={6} cols={3} /></div>}
        {!loading && !error && permissions.length === 0 && (
          <div className="p-12 text-center text-gray-500 dark:text-gray-400 text-sm">No permissions yet. Add one or assign via Role Edit.</div>
        )}
        {!loading && !error && permissions.length > 0 && (
          <div className="overflow-x-auto">
            <table className="table-grid">
              <thead>
                <tr>
                  <th>Permission Name</th>
                  <th>Description</th>
                  <th>Assigned Roles</th>
                  <th className="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {permissions.map((p) => (
                  <tr key={p.id}>
                    <td className="font-medium text-gray-900 dark:text-white">{p.name}</td>
                    <td className="muted">{p.description ?? '—'}</td>
                    <td>
                      <div className="flex flex-wrap gap-1">
                        {(permissionToRoles[p.id] ?? []).length === 0 ? (
                          <span className="text-gray-400 dark:text-gray-500 text-xs">—</span>
                        ) : (
                          (permissionToRoles[p.id] ?? []).map((r) => (
                            <span
                              key={r}
                              className="inline-flex px-2 py-0.5 text-xs font-medium rounded-full bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300"
                            >
                              {r}
                            </span>
                          ))
                        )}
                      </div>
                    </td>
                    <td className="text-right">
                      <div className="table-actions">
                        <button type="button" onClick={() => setEditPermission(p)} className="table-link">Edit</button>
                        <button type="button" onClick={() => setDeletePermissionState(p)} className="table-link-danger">Delete</button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {createOpen && (
        <CreatePermissionModal
          onClose={() => setCreateOpen(false)}
          onSuccess={() => { setCreateOpen(false); loadPermissions(); addToast('success', 'Permission created.'); }}
        />
      )}
      {editPermission && (
        <EditPermissionModal
          permission={editPermission}
          onClose={() => setEditPermission(null)}
          onSuccess={() => { setEditPermission(null); loadPermissions(); addToast('success', 'Permission updated.'); }}
        />
      )}
      {deletePermissionState && (
        <DeletePermissionModal
          permission={deletePermissionState}
          onClose={() => setDeletePermissionState(null)}
          onSuccess={() => { setDeletePermissionState(null); loadPermissions(); addToast('success', 'Permission deleted.'); }}
        />
      )}
    </div>
  );
}

function CreatePermissionModal({ onClose, onSuccess }: { onClose: () => void; onSuccess: () => void }) {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    if (!name.trim()) { setFormError('Name is required.'); return; }
    setSubmitting(true);
    createPermission({ name: name.trim(), description: description.trim() || undefined })
      .then((res) => {
        if (res.success) onSuccess();
        else setFormError(res.error?.message ?? res.message ?? 'Failed to create permission.');
      })
      .catch(() => setFormError('Network or server error.'))
      .finally(() => setSubmitting(false));
  };

  return (
    <Modal open={true} title="Add Permission" onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-4">
        {formError && <div className="alert-error">{formError}</div>}
        <div className="form-group">
          <label htmlFor="cp-name" className="input-label">Permission name *</label>
          <input id="cp-name" type="text" value={name} onChange={(e) => setName(e.target.value)} className="input" required maxLength={100} />
        </div>
        <div className="form-group">
          <label htmlFor="cp-desc" className="input-label">Description</label>
          <input id="cp-desc" type="text" value={description} onChange={(e) => setDescription(e.target.value)} className="input" maxLength={255} />
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="submit" disabled={submitting} className="btn-primary">{submitting ? 'Creating…' : 'Create'}</button>
        </div>
      </form>
    </Modal>
  );
}

function EditPermissionModal({ permission, onClose, onSuccess }: { permission: Permission; onClose: () => void; onSuccess: () => void }) {
  const [name, setName] = useState(permission.name);
  const [description, setDescription] = useState(permission.description ?? '');
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    if (!name.trim()) { setFormError('Name is required.'); return; }
    setSubmitting(true);
    updatePermission(permission.id, { name: name.trim(), description: description.trim() || undefined })
      .then((res) => {
        if (res.success) onSuccess();
        else setFormError(res.error?.message ?? res.message ?? 'Failed to update permission.');
      })
      .catch(() => setFormError('Network or server error.'))
      .finally(() => setSubmitting(false));
  };

  return (
    <Modal open={true} title="Edit Permission" onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-4">
        {formError && <div className="alert-error">{formError}</div>}
        <div className="form-group">
          <label htmlFor="ep-name" className="input-label">Permission name *</label>
          <input id="ep-name" type="text" value={name} onChange={(e) => setName(e.target.value)} className="input" required maxLength={100} />
        </div>
        <div className="form-group">
          <label htmlFor="ep-desc" className="input-label">Description</label>
          <input id="ep-desc" type="text" value={description} onChange={(e) => setDescription(e.target.value)} className="input" maxLength={255} />
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="submit" disabled={submitting} className="btn-primary">{submitting ? 'Saving…' : 'Save'}</button>
        </div>
      </form>
    </Modal>
  );
}

function DeletePermissionModal({ permission, onClose, onSuccess }: { permission: Permission; onClose: () => void; onSuccess: () => void }) {
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const handleConfirm = () => {
    setFormError(null);
    setSubmitting(true);
    deletePermission(permission.id)
      .then((res) => {
        if (res.success) onSuccess();
        else setFormError(res.error?.message ?? res.message ?? 'Failed to delete permission.');
      })
      .catch(() => setFormError('Network or server error.'))
      .finally(() => setSubmitting(false));
  };

  return (
    <Modal open={true} title="Delete Permission" onClose={onClose}>
      <div className="space-y-4">
        {formError && <div className="alert-error">{formError}</div>}
        <p className="text-gray-600 dark:text-gray-400">
          Delete permission <strong className="text-gray-900 dark:text-white">{permission.name}</strong>? This will remove it from all roles.
        </p>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="button" onClick={handleConfirm} disabled={submitting} className="btn-danger">
            {submitting ? 'Deleting…' : 'Delete'}
          </button>
        </div>
      </div>
    </Modal>
  );
}
