import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export function SuperAdminRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, user } = useAuth();
  const isSuperAdmin = (user?.roles ?? []).some((r) => r?.toLowerCase() === 'superadmin');

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (!isSuperAdmin) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
