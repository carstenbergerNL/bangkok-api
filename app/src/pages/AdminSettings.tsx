import { useCallback, useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { createUser, deleteUser, getUsers, restoreUser, updateUser, type UpdateUserRequest } from '../services/userService';
import { addToast } from '../utils/toast';
import type { User } from '../models/User';
import { Modal } from '../components/Modal';

type ModalState = { type: 'create' } | { type: 'edit'; user: User } | { type: 'delete'; user: User } | null;

const ROLES = ['User', 'Admin'];

export function AdminSettings() {
  const { user } = useAuth();
  const [users, setUsers] = useState<User[]>([]);
  const [deletedUsers, setDeletedUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [modal, setModal] = useState<ModalState>(null);
  const [restoringId, setRestoringId] = useState<string | null>(null);

  const loadUsers = useCallback(() => {
    setLoading(true);
    setError(null);
    getUsers(1, 500, true)
      .then((res) => {
        if (res.success && res.data?.items) {
          const items = res.data.items;
          setUsers(items.filter((u) => !u.isDeleted));
          setDeletedUsers(items.filter((u) => u.isDeleted === true));
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

  useEffect(() => {
    loadUsers();
  }, [loadUsers]);

  return (
    <div className="space-y-8">
      <div className="page-header">
        <h1>Admin Settings</h1>
        <p>Administrator-only configuration and user management.</p>
      </div>

      <div className="card card-body">
        <h2 className="card-header">Admin overview</h2>
        <p className="card-description mt-1">
          You are signed in as an administrator ({user?.email ?? '—'}). Manage users below and use this area for other admin tasks.
        </p>
      </div>

      <div className="card overflow-hidden">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 px-6 pt-6 pb-4">
          <div>
            <h2 className="text-lg font-medium text-gray-900 dark:text-white">Users</h2>
            <p className="card-description mt-0.5">Manage users from the Bangkok API.</p>
          </div>
          <button type="button" onClick={() => setModal({ type: 'create' })} className="btn-primary shrink-0">
            Add user
          </button>
        </div>
        {loading && <div className="px-6 pb-8 text-center text-gray-500 dark:text-gray-400 text-sm">Loading users…</div>}
        {error && !loading && <div className="px-6 pb-6"><div className="alert-error">{error}</div></div>}
        {!loading && !error && users.length === 0 && (
          <div className="px-6 pb-8 text-center text-gray-500 dark:text-gray-400 text-sm">No users found.</div>
        )}
        {!loading && !error && users.length > 0 && (
          <div className="table-container">
            <table className="table-grid">
              <thead>
                <tr>
                  <th>Email</th>
                  <th>Display name</th>
                  <th>Roles</th>
                  <th>Active</th>
                  <th className="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {users.map((u) => {
                  const isSelf = user?.email != null && u.email != null && user.email.trim().toLowerCase() === u.email.trim().toLowerCase();
                  return (
                    <tr key={u.id}>
                      <td>{u.email}</td>
                      <td className="muted">{u.displayName ?? '—'}</td>
                      <td className="muted">{(u.roles ?? []).join(', ') || '—'}</td>
                      <td>
                        <span className={u.isActive ? 'text-emerald-600 dark:text-emerald-400 font-medium' : 'text-gray-400 dark:text-gray-500'}>
                          {u.isActive ? 'Yes' : 'No'}
                        </span>
                      </td>
                      <td className="text-right">
                        <div className="table-actions">
                          <button type="button" onClick={() => setModal({ type: 'edit', user: u })} className="table-link">
                            Edit
                          </button>
                          {!isSelf && (
                            <button type="button" onClick={() => setModal({ type: 'delete', user: u })} className="table-link-danger">
                              Delete
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

      {deletedUsers.length > 0 && (
        <div className="card overflow-hidden">
          <div className="px-6 pt-6 pb-2">
            <h2 className="text-lg font-medium text-gray-900 dark:text-white">Deleted users</h2>
            <p className="card-description mt-0.5">Restore a soft-deleted user so they can log in again.</p>
          </div>
          <div className="table-container">
            <table className="table-grid">
              <thead>
                <tr>
                  <th>Email</th>
                  <th>Display name</th>
                  <th>Roles</th>
                  <th>Deleted at</th>
                  <th className="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {deletedUsers.map((u) => (
                  <tr key={u.id}>
                    <td>{u.email}</td>
                    <td className="muted">{u.displayName ?? '—'}</td>
                    <td className="muted">{(u.roles ?? []).join(', ') || '—'}</td>
                    <td className="muted">{u.deletedAtUtc ? new Date(u.deletedAtUtc).toLocaleString() : '—'}</td>
                    <td className="text-right">
                      <button
                        type="button"
                        onClick={() => {
                          setRestoringId(u.id);
                          restoreUser(u.id)
                            .then((res) => {
                              if (res.success) {
                                setDeletedUsers((prev) => prev.filter((x) => x.id !== u.id));
                                loadUsers();
                                addToast('success', 'User restored.');
                              } else {
                                addToast('error', res.error?.message ?? res.message ?? 'Failed to restore user.');
                              }
                            })
                            .catch(() => addToast('error', 'Failed to restore user.'))
                            .finally(() => setRestoringId(null));
                        }}
                        disabled={restoringId !== null}
                        className="table-link disabled:opacity-50 disabled:cursor-not-allowed"
                      >
                        {restoringId === u.id ? 'Restoring…' : 'Restore'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <div className="card card-body">
        <h2 className="card-header">Configuration</h2>
        <p className="card-description mt-1">Application configuration options can be added here.</p>
      </div>

      {modal?.type === 'create' && (
        <CreateUserModal
          onClose={() => setModal(null)}
          onSuccess={() => {
            setModal(null);
            loadUsers();
            addToast('success', 'User created.');
          }}
        />
      )}
      {modal?.type === 'edit' && (
        <EditUserModal
          user={modal.user}
          onClose={() => setModal(null)}
          onSuccess={() => {
            setModal(null);
            loadUsers();
            addToast('success', 'User updated.');
          }}
        />
      )}
      {modal?.type === 'delete' && (
        <DeleteUserModal
          user={modal.user}
          onClose={() => setModal(null)}
          onSuccess={() => {
            setModal(null);
            loadUsers();
            addToast('success', 'User deleted.');
          }}
        />
      )}
    </div>
  );
}

function CreateUserModal({
  onClose,
  onSuccess,
}: {
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [role, setRole] = useState('User');
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    if (!email.trim()) {
      setFormError('Email is required.');
      return;
    }
    if (!password || password.length < 8) {
      setFormError('Password must be at least 8 characters.');
      return;
    }
    setSubmitting(true);
    createUser({ email: email.trim(), password, displayName: displayName.trim() || undefined, role })
      .then((res) => {
        if (res.success) {
          onSuccess();
          return;
        }
        setFormError(res.error?.message ?? res.message ?? 'Failed to create user.');
      })
      .catch(() => setFormError('Network or server error.'))
      .finally(() => setSubmitting(false));
  };

  return (
    <Modal open={true} title="Add user" onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-1">
        {formError && <div className="alert-error mb-4">{formError}</div>}
        <div className="form-group">
          <label htmlFor="create-email" className="input-label">Email *</label>
          <input id="create-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} className="input" required autoComplete="email" />
        </div>
        <div className="form-group">
          <label htmlFor="create-password" className="input-label">Password * (min 8 characters)</label>
          <input id="create-password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} className="input" required minLength={8} autoComplete="new-password" />
        </div>
        <div className="form-group">
          <label htmlFor="create-displayName" className="input-label">Display name</label>
          <input id="create-displayName" type="text" value={displayName} onChange={(e) => setDisplayName(e.target.value)} className="input" maxLength={256} />
        </div>
        <div className="form-group">
          <label htmlFor="create-role" className="input-label">Role</label>
          <select id="create-role" value={role} onChange={(e) => setRole(e.target.value)} className="input">
            {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
          </select>
        </div>
        <div className="flex justify-end gap-2 pt-4">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="submit" disabled={submitting} className="btn-primary">{submitting ? 'Creating…' : 'Create user'}</button>
        </div>
      </form>
    </Modal>
  );
}

function EditUserModal({
  user,
  onClose,
  onSuccess,
}: {
  user: User;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const { user: currentUser } = useAuth();
  const isEditingSelf = currentUser?.email != null && user.email != null && currentUser.email.trim().toLowerCase() === user.email.trim().toLowerCase();
  const [email, setEmail] = useState(user.email);
  const [displayName, setDisplayName] = useState(user.displayName ?? '');
  const [role, setRole] = useState(user.roles?.[0] ?? 'User');
  const [isActive, setIsActive] = useState(user.isActive);
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    const payload: UpdateUserRequest = {
      email: email.trim() || undefined,
      displayName: displayName.trim() || undefined,
      role,
      ...(isEditingSelf ? {} : { isActive }),
    };
    setSubmitting(true);
    updateUser(user.id, payload)
      .then((res) => {
        if (res.success) {
          onSuccess();
          return;
        }
        setFormError(res.error?.message ?? res.message ?? 'Failed to update user.');
      })
      .catch(() => setFormError('Network or server error.'))
      .finally(() => setSubmitting(false));
  };

  return (
    <Modal open={true} title="Edit user" onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-1">
        {formError && <div className="alert-error mb-4">{formError}</div>}
        <div className="form-group">
          <label htmlFor="edit-email" className="input-label">Email</label>
          <input id="edit-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} className="input" autoComplete="email" />
        </div>
        <div className="form-group">
          <label htmlFor="edit-displayName" className="input-label">Display name</label>
          <input id="edit-displayName" type="text" value={displayName} onChange={(e) => setDisplayName(e.target.value)} className="input" maxLength={256} />
        </div>
        <div className="form-group">
          <label htmlFor="edit-role" className="input-label">Role</label>
          <select id="edit-role" value={role} onChange={(e) => setRole(e.target.value)} className="input">
            {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
          </select>
        </div>
        <div className="flex items-center gap-2 py-1">
          <input
            id="edit-isActive"
            type="checkbox"
            checked={isActive}
            onChange={(e) => setIsActive(e.target.checked)}
            disabled={isEditingSelf}
            className="rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500 disabled:opacity-50 disabled:cursor-not-allowed"
          />
          <label htmlFor="edit-isActive" className="text-sm font-medium text-gray-700 dark:text-gray-300">
            Active
            {isEditingSelf && <span className="ml-1 text-gray-400 dark:text-gray-500 font-normal">(you cannot change your own)</span>}
          </label>
        </div>
        <div className="flex justify-end gap-2 pt-4">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="submit" disabled={submitting} className="btn-primary">{submitting ? 'Saving…' : 'Save'}</button>
        </div>
      </form>
    </Modal>
  );
}

function DeleteUserModal({
  user,
  onClose,
  onSuccess,
}: {
  user: User;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const { user: currentUser } = useAuth();
  const isSelf = currentUser?.email != null && user.email != null && currentUser.email.trim().toLowerCase() === user.email.trim().toLowerCase();
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const handleConfirm = () => {
    if (isSelf) return;
    setFormError(null);
    setSubmitting(true);
    deleteUser(user.id)
      .then((res) => {
        if (res.success) {
          onSuccess();
          return;
        }
        setFormError(res.error?.message ?? res.message ?? 'Failed to delete user.');
      })
      .catch(() => setFormError('Network or server error.'))
      .finally(() => setSubmitting(false));
  };

  return (
    <Modal open={true} title="Delete user" onClose={onClose}>
      <div className="space-y-4">
        {formError && <div className="alert-error">{formError}</div>}
        {isSelf ? (
          <p className="text-sm text-amber-600 dark:text-amber-400">You cannot delete your own account.</p>
        ) : (
          <p className="text-gray-600 dark:text-gray-400 leading-relaxed">
            Are you sure you want to delete <strong className="text-gray-900 dark:text-white">{user.email}</strong>? This will soft-delete the user (they cannot log in until restored by an admin).
          </p>
        )}
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          {!isSelf && (
            <button type="button" onClick={handleConfirm} disabled={submitting} className="btn-danger">
              {submitting ? 'Deleting…' : 'Delete'}
            </button>
          )}
        </div>
      </div>
    </Modal>
  );
}
