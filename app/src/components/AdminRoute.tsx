import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export function AdminRoute({ children }: { children: React.ReactNode }) {
  const { user, isAuthenticated } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  const isAdmin = user?.role != null && user.role.localeCompare('Admin', undefined, { sensitivity: 'accent' }) === 0;
  if (!isAdmin) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
