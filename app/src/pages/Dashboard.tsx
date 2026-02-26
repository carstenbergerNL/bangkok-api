import { useAuth } from '../context/AuthContext';

export function Dashboard() {
  const { user } = useAuth();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-gray-800 dark:text-gray-100">Dashboard</h1>
        <p className="text-gray-500 dark:text-gray-400 mt-1">Welcome back. Here’s an overview.</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 shadow-soft p-6">
          <h2 className="text-sm font-medium text-gray-500 dark:text-gray-400">Signed in as</h2>
          <p className="mt-1 text-lg font-medium text-gray-800 dark:text-gray-100 truncate">{user?.email || '—'}</p>
        </div>
        <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 shadow-soft p-6">
          <h2 className="text-sm font-medium text-gray-500 dark:text-gray-400">Display name</h2>
          <p className="mt-1 text-lg font-medium text-gray-800 dark:text-gray-100">{user?.displayName || '—'}</p>
        </div>
        <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 shadow-soft p-6">
          <h2 className="text-sm font-medium text-gray-500 dark:text-gray-400">Role</h2>
          <p className="mt-1 text-lg font-medium text-gray-800 dark:text-gray-100">{user?.role || '—'}</p>
        </div>
      </div>

      <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 shadow-soft p-6">
        <h2 className="text-lg font-medium text-gray-800 dark:text-gray-100">Getting started</h2>
        <p className="mt-2 text-sm text-gray-600 dark:text-gray-400">
          Use the sidebar to open Users or Settings. This app connects to the Bangkok API with JWT authentication.
        </p>
      </div>
    </div>
  );
}
