import { Navigate } from 'react-router-dom';
import { PERMISSIONS } from '../constants/permissions';
import { useAuth } from '../context/AuthContext';
import { usePermissions } from '../hooks/usePermissions';

export function AdminRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth();
  const { hasPermission } = usePermissions();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (!hasPermission(PERMISSIONS.ViewAdminSettings)) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
