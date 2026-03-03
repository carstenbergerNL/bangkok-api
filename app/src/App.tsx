import { useEffect } from 'react';
import { BrowserRouter, Navigate, Route, Routes, useNavigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './context/AuthContext';
import { PrivateRoute } from './components/PrivateRoute';
import { AdminRoute } from './components/AdminRoute';
import { PermissionRoute } from './components/PermissionRoute';
import { ToastContainer } from './components/Toast';
import { MainLayout } from './layouts/MainLayout';
import { Login } from './pages/Login';
import { Dashboard } from './pages/Dashboard';
import { Profile } from './pages/Profile';
import { AdminSettings } from './pages/AdminSettings';
import { ProjectListPage } from './modules/projects/ProjectListPage';
import { ProjectDetailsPage } from './modules/projects/ProjectDetailsPage';
import { PERMISSIONS } from './constants/permissions';
import { initAuthUnauthorizedHandler } from './services/authService';
import { RouteErrorBoundary } from './components/RouteErrorBoundary';

function LoginOrRedirect() {
  const { isAuthenticated } = useAuth();
  if (isAuthenticated) return <Navigate to="/" replace />;
  return <Login />;
}

function AppRoutes() {
  const navigate = useNavigate();
  const { logout } = useAuth();
  useEffect(() => {
    initAuthUnauthorizedHandler(() => {
      logout();
      navigate('/login', { replace: true });
    });
  }, [navigate, logout]);

  return (
    <Routes>
      <Route path="/login" element={<LoginOrRedirect />} />
      <Route
        path="/"
        element={
          <PrivateRoute>
            <MainLayout />
          </PrivateRoute>
        }
      >
        <Route index element={<Dashboard />} />
        <Route path="profile" element={<Profile />} />
        <Route path="admin-settings" element={<AdminRoute><AdminSettings /></AdminRoute>} />
        <Route path="projects" element={<PermissionRoute permission={PERMISSIONS.ProjectView}><ProjectListPage /></PermissionRoute>} />
        <Route path="projects/:id" element={<PermissionRoute permission={PERMISSIONS.ProjectView}><RouteErrorBoundary><ProjectDetailsPage /></RouteErrorBoundary></PermissionRoute>} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <AppRoutes />
        <ToastContainer />
      </AuthProvider>
    </BrowserRouter>
  );
}
