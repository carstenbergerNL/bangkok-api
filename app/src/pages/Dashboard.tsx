import { useAuth } from '../context/AuthContext';

export function Dashboard() {
  const { user } = useAuth();

  return (
    <div className="space-y-8">
      <div className="page-header">
        <h1>Dashboard</h1>
        <p>Welcome back. Here’s an overview.</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <div className="card card-body">
          <h2 className="card-header text-sm font-medium text-gray-500 dark:text-gray-400">Signed in as</h2>
          <p className="mt-1 text-base font-medium text-gray-900 dark:text-white truncate">{user?.email || '—'}</p>
        </div>
        <div className="card card-body">
          <h2 className="card-header text-sm font-medium text-gray-500 dark:text-gray-400">Display name</h2>
          <p className="mt-1 text-base font-medium text-gray-900 dark:text-white">{user?.displayName || '—'}</p>
        </div>
        <div className="card card-body">
          <h2 className="card-header text-sm font-medium text-gray-500 dark:text-gray-400">Role</h2>
          <p className="mt-1 text-base font-medium text-gray-900 dark:text-white">{user?.role || '—'}</p>
        </div>
      </div>

      <div className="card card-body">
        <h2 className="text-lg font-medium text-gray-900 dark:text-white mb-2">Getting started</h2>
        <p className="text-sm text-gray-600 dark:text-gray-400 leading-relaxed">
          Use the sidebar to open Users or Settings. This app connects to the Bangkok API with JWT authentication.
        </p>
      </div>
    </div>
  );
}
