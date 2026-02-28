import { useCallback, useEffect, useState } from 'react';
import {
  getRoles,
  createRole,
  updateRole,
  deleteRole,
  assignRoleToUser,
} from '../services/roleService';
import { getUsers } from '../services/userService';
import { addToast } from '../utils/toast';
import type { Role } from '../models/Role';
import type { User } from '../models/User';
import { Modal } from '../components/Modal';

type ModalState =
  | { type: 'create' }
  | { type: 'edit'; role: Role }
  | { type: 'delete'; role: Role }
  | { type: 'assign'; role: Role }
  | null;

export function Roles() {
  const [roles, setRoles] = useState<Role[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [modal, setModal] = useState<ModalState>(null);

  const loadRoles = useCallback(() => {
    setLoading(true);
    setError(null);
    getRoles()
      .then((res) => {
        if (res.success && res.data) {
          setRoles(res.data);
        } else {
          setError(res.error?.message ?? res.message ?? 'Failed to load roles.');
        }
      })
      .catch(() => {
        setError('Network or server error.');
        addToast('error', 'Failed to load roles.');
      })
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    loadRoles();
  }, [loadRoles]);

  return (
    <div className="space-y-8">
      <div className="page-header">
        <h1>Roles</h1>
        <p>Manage roles and permissions. Assign roles to users.</p>
      </div>

      <div className="card overflow-hidden">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 px-6 pt-6 pb-4">
          <div>
            <h2 className="text-lg font-medium text-gray-900 dark:text-white">Roles</h2>
            <p className="card-description mt-0.5">Create, edit, and delete roles. Assign roles to users.</p>
          </div>
          <button type="button" onClick={() => setModal({ type: 'create' })} className="btn-primary shrink-0">
            Add role
          </button>
        </div>
        {loading && <div className="px-6 pb-8 text-center text-gray-500 dark:text-gray-400 text-sm">Loading roles…</div>}
        {error && !loading && <div className="px-6 pb-6"><div className="alert-error">{error}</div></div>}
        {!loading && !error && roles.length === 0 && (
          <div className="px-6 pb-8 text-center text-gray-500 dark:text-gray-400 text-sm">No roles found.</div>
        )}
        {!loading && !error && roles.length > 0 && (
          <div className="table-container">
            <table className="table-grid">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Description</th>
                  <th>Created</th>
                  <th className="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {roles.map((r) => (
                  <tr key={r.id}>
                    <td className="font-medium text-gray-900 dark:text-white">{r.name}</td>
                    <td className="muted">{r.description ?? '—'}</td>
                    <td className="muted">{r.createdAt ? new Date(r.createdAt).toLocaleDateString() : '—'}</td>
                    <td className="text-right">
                      <div className="table-actions">
                        <button type="button" onClick={() => setModal({ type: 'edit', role: r })} className="table-link">
                          Edit
                        </button>
                        <button type="button" onClick={() => setModal({ type: 'assign', role: r })} className="table-link">
                          Assign to user
                        </button>
                        <button type="button" onClick={() => setModal({ type: 'delete', role: r })} className="table-link-danger">
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {modal?.type === 'create' && (
        <CreateRoleModal
          onClose={() => setModal(null)}
          onSuccess={() => {
            setModal(null);
            loadRoles();
            addToast('success', 'Role created.');
          }}
        />
      )}
      {modal?.type === 'edit' && (
        <EditRoleModal
          role={modal.role}
          onClose={() => setModal(null)}
          onSuccess={() => {
            setModal(null);
            loadRoles();
            addToast('success', 'Role updated.');
          }}
        />
      )}
      {modal?.type === 'delete' && (
        <DeleteRoleModal
          role={modal.role}
          onClose={() => setModal(null)}
          onSuccess={() => {
            setModal(null);
            loadRoles();
            addToast('success', 'Role deleted.');
          }}
        />
      )}
      {modal?.type === 'assign' && (
        <AssignRoleModal
          role={modal.role}
          onClose={() => setModal(null)}
          onSuccess={() => {
            setModal(null);
            addToast('success', 'Role assigned.');
          }}
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
    if (!name.trim()) {
      setFormError('Name is required.');
      return;
    }
    setSubmitting(true);
    createRole({ name: name.trim(), description: description.trim() || undefined })
      .then((res) => {
        if (res.success) {
          onSuccess();
          return;
        }
        setFormError(res.error?.message ?? res.message ?? 'Failed to create role.');
      })
      .catch(() => setFormError('Network or server error.'))
      .finally(() => setSubmitting(false));
  };

  return (
    <Modal open={true} title="Add role" onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-1">
        {formError && <div className="alert-error mb-4">{formError}</div>}
        <div className="form-group">
          <label htmlFor="create-role-name" className="input-label">Name *</label>
          <input id="create-role-name" type="text" value={name} onChange={(e) => setName(e.target.value)} className="input" required maxLength={100} />
        </div>
        <div className="form-group">
          <label htmlFor="create-role-desc" className="input-label">Description</label>
          <input id="create-role-desc" type="text" value={description} onChange={(e) => setDescription(e.target.value)} className="input" maxLength={255} />
        </div>
        <div className="flex justify-end gap-2 pt-4">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="submit" disabled={submitting} className="btn-primary">{submitting ? 'Creating…' : 'Create role'}</button>
        </div>
      </form>
    </Modal>
  );
}

function EditRoleModal({ role, onClose, onSuccess }: { role: Role; onClose: () => void; onSuccess: () => void }) {
  const [name, setName] = useState(role.name);
  const [description, setDescription] = useState(role.description ?? '');
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    if (!name.trim()) {
      setFormError('Name is required.');
      return;
    }
    setSubmitting(true);
    updateRole(role.id, { name: name.trim(), description: description.trim() || undefined })
      .then((res) => {
        if (res.success) {
          onSuccess();
          return;
        }
        setFormError(res.error?.message ?? res.message ?? 'Failed to update role.');
      })
      .catch(() => setFormError('Network or server error.'))
      .finally(() => setSubmitting(false));
  };

  return (
    <Modal open={true} title="Edit role" onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-1">
        {formError && <div className="alert-error mb-4">{formError}</div>}
        <div className="form-group">
          <label htmlFor="edit-role-name" className="input-label">Name *</label>
          <input id="edit-role-name" type="text" value={name} onChange={(e) => setName(e.target.value)} className="input" required maxLength={100} />
        </div>
        <div className="form-group">
          <label htmlFor="edit-role-desc" className="input-label">Description</label>
          <input id="edit-role-desc" type="text" value={description} onChange={(e) => setDescription(e.target.value)} className="input" maxLength={255} />
        </div>
        <div className="flex justify-end gap-2 pt-4">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="submit" disabled={submitting} className="btn-primary">{submitting ? 'Saving…' : 'Save'}</button>
        </div>
      </form>
    </Modal>
  );
}

function DeleteRoleModal({ role, onClose, onSuccess }: { role: Role; onClose: () => void; onSuccess: () => void }) {
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const handleConfirm = () => {
    setFormError(null);
    setSubmitting(true);
    deleteRole(role.id)
      .then((res) => {
        if (res.success) {
          onSuccess();
          return;
        }
        setFormError(res.error?.message ?? res.message ?? 'Failed to delete role.');
      })
      .catch(() => setFormError('Network or server error.'))
      .finally(() => setSubmitting(false));
  };

  return (
    <Modal open={true} title="Delete role" onClose={onClose}>
      <div className="space-y-4">
        {formError && <div className="alert-error">{formError}</div>}
        <p className="text-gray-600 dark:text-gray-400 leading-relaxed">
          Are you sure you want to delete role <strong className="text-gray-900 dark:text-white">{role.name}</strong>? This will remove it from all users.
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

function AssignRoleModal({ role, onClose, onSuccess }: { role: Role; onClose: () => void; onSuccess: () => void }) {
  const [users, setUsers] = useState<User[]>([]);
  const [selectedUserId, setSelectedUserId] = useState('');
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    getUsers(1, 500, false)
      .then((res) => {
        if (res.success && res.data?.items) {
          setUsers(res.data.items.filter((u) => !u.isDeleted));
        }
      })
      .finally(() => setLoading(false));
  }, []);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    if (!selectedUserId) {
      setFormError('Please select a user.');
      return;
    }
    setSubmitting(true);
    assignRoleToUser(selectedUserId, role.id)
      .then(() => onSuccess())
      .catch(() => {
        setFormError('Failed to assign role.');
      })
      .finally(() => setSubmitting(false));
  };

  return (
    <Modal open={true} title={`Assign role: ${role.name}`} onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-1">
        {formError && <div className="alert-error mb-4">{formError}</div>}
        <div className="form-group">
          <label htmlFor="assign-user" className="input-label">User</label>
          <select id="assign-user" value={selectedUserId} onChange={(e) => setSelectedUserId(e.target.value)} className="input">
            <option value="">Select a user…</option>
            {users.map((u) => (
              <option key={u.id} value={u.id}>{u.email} {u.displayName ? `(${u.displayName})` : ''}</option>
            ))}
          </select>
        </div>
        {loading && <p className="text-sm text-gray-500 dark:text-gray-400">Loading users…</p>}
        <div className="flex justify-end gap-2 pt-4">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="submit" disabled={submitting || loading || !selectedUserId} className="btn-primary">
            {submitting ? 'Assigning…' : 'Assign'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
