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
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-gray-800 dark:text-gray-100">Admin Settings</h1>
        <p className="text-gray-500 dark:text-gray-400 mt-1">Administrator-only configuration and user management.</p>
      </div>

      <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 shadow-soft p-6">
        <h2 className="text-lg font-medium text-gray-800 dark:text-gray-100 mb-2">Admin overview</h2>
        <p className="text-sm text-gray-600 dark:text-gray-400">
          You are signed in as an administrator ({user?.email ?? '—'}). Manage users below and use this area for other admin tasks.
        </p>
      </div>

      <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 shadow-soft overflow-hidden">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 px-6 pt-6 pb-2">
          <div>
            <h2 className="text-lg font-medium text-gray-800 dark:text-gray-100">Users</h2>
            <p className="text-sm text-gray-500 dark:text-gray-400">Manage users from the Bangkok API.</p>
          </div>
          <button
            type="button"
            onClick={() => setModal({ type: 'create' })}
            className="shrink-0 px-4 py-2 rounded-lg bg-primary-600 hover:bg-primary-700 text-white text-sm font-medium transition-colors"
          >
            Add user
          </button>
        </div>
        {loading && <div className="px-6 pb-6 text-center text-gray-500">Loading users…</div>}
        {error && !loading && <div className="px-6 pb-6 text-red-600">{error}</div>}
        {!loading && !error && users.length === 0 && (
          <div className="px-6 pb-6 text-center text-gray-500">No users found.</div>
        )}
        {!loading && !error && users.length > 0 && (
          <div className="overflow-x-auto">
            <table className="w-full text-sm text-left">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50">
                  <th className="px-4 py-3 font-medium text-gray-700 dark:text-gray-300">Email</th>
                  <th className="px-4 py-3 font-medium text-gray-700 dark:text-gray-300">Display name</th>
                  <th className="px-4 py-3 font-medium text-gray-700 dark:text-gray-300">Role</th>
                  <th className="px-4 py-3 font-medium text-gray-700 dark:text-gray-300">Active</th>
                  <th className="px-4 py-3 font-medium text-gray-700 dark:text-gray-300 text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {users.map((u) => {
                  const isSelf = user?.email != null && u.email != null && user.email.trim().toLowerCase() === u.email.trim().toLowerCase();
                  return (
                    <tr key={u.id} className="border-b border-gray-100 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-gray-800/30 transition-colors">
                      <td className="px-4 py-3 text-gray-800 dark:text-gray-200">{u.email}</td>
                      <td className="px-4 py-3 text-gray-600 dark:text-gray-400">{u.displayName ?? '—'}</td>
                      <td className="px-4 py-3 text-gray-600 dark:text-gray-400">{u.role}</td>
                      <td className="px-4 py-3">
                        <span className={u.isActive ? 'text-emerald-600' : 'text-gray-400'}>
                          {u.isActive ? 'Yes' : 'No'}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-right">
                        <button
                          type="button"
                          onClick={() => setModal({ type: 'edit', user: u })}
                          className="text-primary-600 dark:text-primary-400 hover:underline mr-3"
                        >
                          Edit
                        </button>
                        {!isSelf && (
                          <button
                            type="button"
                            onClick={() => setModal({ type: 'delete', user: u })}
                            className="text-red-600 dark:text-red-400 hover:underline"
                          >
                            Delete
                          </button>
                        )}
                        {isSelf && (
                          <span className="text-gray-400 dark:text-gray-500 text-xs">(you)</span>
                        )}
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
        <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 shadow-soft overflow-hidden">
          <h2 className="text-lg font-medium text-gray-800 dark:text-gray-100 px-6 pt-6 pb-2">Deleted users</h2>
          <p className="text-sm text-gray-500 dark:text-gray-400 px-6 pb-4">Restore a soft-deleted user so they can log in again.</p>
          <div className="overflow-x-auto">
            <table className="w-full text-sm text-left">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50">
                  <th className="px-4 py-3 font-medium text-gray-700 dark:text-gray-300">Email</th>
                  <th className="px-4 py-3 font-medium text-gray-700 dark:text-gray-300">Display name</th>
                  <th className="px-4 py-3 font-medium text-gray-700 dark:text-gray-300">Role</th>
                  <th className="px-4 py-3 font-medium text-gray-700 dark:text-gray-300">Deleted at</th>
                  <th className="px-4 py-3 font-medium text-gray-700 dark:text-gray-300 text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {deletedUsers.map((u) => (
                  <tr key={u.id} className="border-b border-gray-100 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-gray-800/30 transition-colors">
                    <td className="px-4 py-3 text-gray-800 dark:text-gray-200">{u.email}</td>
                    <td className="px-4 py-3 text-gray-600 dark:text-gray-400">{u.displayName ?? '—'}</td>
                    <td className="px-4 py-3 text-gray-600 dark:text-gray-400">{u.role}</td>
                    <td className="px-4 py-3 text-gray-500 dark:text-gray-400">
                      {u.deletedAtUtc ? new Date(u.deletedAtUtc).toLocaleString() : '—'}
                    </td>
                    <td className="px-4 py-3 text-right">
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
                        className="text-primary-600 dark:text-primary-400 hover:underline disabled:opacity-50 disabled:cursor-not-allowed"
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

      <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 shadow-soft p-6">
        <h2 className="text-base font-medium text-gray-800 dark:text-gray-100">Configuration</h2>
        <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">Application configuration options can be added here.</p>
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
      <form onSubmit={handleSubmit} className="space-y-4">
        {formError && (
          <p className="text-sm text-red-600 dark:text-red-400">{formError}</p>
        )}
        <div>
          <label htmlFor="create-email" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Email *
          </label>
          <input
            id="create-email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-200 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            required
            autoComplete="email"
          />
        </div>
        <div>
          <label htmlFor="create-password" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Password * (min 8 characters)
          </label>
          <input
            id="create-password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-200 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            required
            minLength={8}
            autoComplete="new-password"
          />
        </div>
        <div>
          <label htmlFor="create-displayName" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Display name
          </label>
          <input
            id="create-displayName"
            type="text"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            className="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-200 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            maxLength={256}
          />
        </div>
        <div>
          <label htmlFor="create-role" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Role
          </label>
          <select
            id="create-role"
            value={role}
            onChange={(e) => setRole(e.target.value)}
            className="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-200 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
          >
            {ROLES.map((r) => (
              <option key={r} value={r}>{r}</option>
            ))}
          </select>
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
            Cancel
          </button>
          <button type="submit" disabled={submitting} className="px-4 py-2 rounded-lg bg-primary-600 hover:bg-primary-700 text-white font-medium disabled:opacity-50 disabled:cursor-not-allowed transition-colors">
            {submitting ? 'Creating…' : 'Create user'}
          </button>
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
  const [role, setRole] = useState(user.role);
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
      <form onSubmit={handleSubmit} className="space-y-4">
        {formError && (
          <p className="text-sm text-red-600 dark:text-red-400">{formError}</p>
        )}
        <div>
          <label htmlFor="edit-email" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Email
          </label>
          <input
            id="edit-email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-200 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            autoComplete="email"
          />
        </div>
        <div>
          <label htmlFor="edit-displayName" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Display name
          </label>
          <input
            id="edit-displayName"
            type="text"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            className="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-200 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            maxLength={256}
          />
        </div>
        <div>
          <label htmlFor="edit-role" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Role
          </label>
          <select
            id="edit-role"
            value={role}
            onChange={(e) => setRole(e.target.value)}
            className="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-200 focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
          >
            {ROLES.map((r) => (
              <option key={r} value={r}>{r}</option>
            ))}
          </select>
        </div>
        <div className="flex items-center gap-2">
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
            {isEditingSelf && (
              <span className="ml-1 text-gray-400 dark:text-gray-500 font-normal">(you cannot change your own)</span>
            )}
          </label>
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
            Cancel
          </button>
          <button type="submit" disabled={submitting} className="px-4 py-2 rounded-lg bg-primary-600 hover:bg-primary-700 text-white font-medium disabled:opacity-50 disabled:cursor-not-allowed transition-colors">
            {submitting ? 'Saving…' : 'Save'}
          </button>
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
        {formError && (
          <p className="text-sm text-red-600 dark:text-red-400">{formError}</p>
        )}
        {isSelf ? (
          <p className="text-sm text-amber-600 dark:text-amber-400">You cannot delete your own account.</p>
        ) : (
          <p className="text-gray-600 dark:text-gray-400">
            Are you sure you want to delete <strong className="text-gray-800 dark:text-gray-200">{user.email}</strong>? This will soft-delete the user (they cannot log in until restored by an admin).
          </p>
        )}
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
            Cancel
          </button>
          {!isSelf && (
            <button type="button" onClick={handleConfirm} disabled={submitting} className="px-4 py-2 rounded-lg bg-red-600 hover:bg-red-700 text-white font-medium disabled:opacity-50 disabled:cursor-not-allowed transition-colors">
            {submitting ? 'Deleting…' : 'Delete'}
          </button>
          )}
        </div>
      </div>
    </Modal>
  );
}
