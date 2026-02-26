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
          <h2 className="card-description text-xs font-semibold uppercase tracking-wider">Signed in as</h2>
          <p className="mt-1 text-base font-semibold truncate" style={{ color: 'var(--card-header-color, #323130)' }}>{user?.email || '—'}</p>
        </div>
        <div className="card card-body">
          <h2 className="card-description text-xs font-semibold uppercase tracking-wider">Display name</h2>
          <p className="mt-1 text-base font-semibold" style={{ color: 'var(--card-header-color, #323130)' }}>{user?.displayName || '—'}</p>
        </div>
        <div className="card card-body">
          <h2 className="card-description text-xs font-semibold uppercase tracking-wider">Role</h2>
          <p className="mt-1 text-base font-semibold" style={{ color: 'var(--card-header-color, #323130)' }}>{user?.role || '—'}</p>
        </div>
      </div>

      <div className="card card-body">
        <h2 className="card-header mb-2">Getting started</h2>
        <p className="card-description leading-relaxed">
          Use the sidebar to open Profile or Admin Settings. This app connects to the Bangkok API with JWT authentication.
        </p>
      </div>
    </div>
  );
}
