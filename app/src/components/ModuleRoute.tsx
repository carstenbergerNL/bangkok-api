import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useActiveModules } from '../hooks/useActiveModules';

interface ModuleRouteProps {
  children: React.ReactNode;
  moduleKey: string;
}

/**
 * Allows access if the user has the given module in their active module list (tenant + user-level access).
 * Use for routes that are gated by module access (e.g. ProjectManagement) so users granted access
 * via Users & Access can reach the page; API still enforces module access.
 */
export function ModuleRoute({ children, moduleKey }: ModuleRouteProps) {
  const { isAuthenticated } = useAuth();
  const { activeModuleKeys, loading } = useActiveModules();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (loading) {
    return null;
  }

  if (!activeModuleKeys.includes(moduleKey)) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
