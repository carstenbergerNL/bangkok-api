import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { usePermissions } from '../hooks/usePermissions';

interface PermissionRouteProps {
  children: React.ReactNode;
  permission: string;
}

export function PermissionRoute({ children, permission }: PermissionRouteProps) {
  const { isAuthenticated } = useAuth();
  const { hasPermission } = usePermissions();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (!hasPermission(permission)) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
