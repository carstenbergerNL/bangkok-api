import { useEffect } from 'react';
import { BrowserRouter, Navigate, Route, Routes, useNavigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './context/AuthContext';
import { PrivateRoute } from './components/PrivateRoute';
import { AdminRoute } from './components/AdminRoute';
import { SuperAdminRoute } from './components/SuperAdminRoute';
import { PermissionRoute } from './components/PermissionRoute';
import { ModuleRoute } from './components/ModuleRoute';
import { ToastContainer } from './components/Toast';
import { MainLayout } from './layouts/MainLayout';
import { Login } from './pages/Login';
import { Dashboard } from './pages/Dashboard';
import { Profile } from './pages/Profile';
import { Billing } from './pages/Billing';
import { BillingReturn } from './pages/BillingReturn';
import { AdminSettings } from './pages/AdminSettings';
import { PlatformDashboard } from './pages/PlatformDashboard';
import { ProjectListPage } from './modules/projects/ProjectListPage';
import { ProjectDetailsPage } from './modules/projects/ProjectDetailsPage';
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
        <Route path="billing" element={<Billing />} />
        <Route path="billing/:status" element={<BillingReturn />} />
        <Route path="admin-settings" element={<AdminRoute><AdminSettings /></AdminRoute>} />
        <Route path="platform-dashboard" element={<SuperAdminRoute><PlatformDashboard /></SuperAdminRoute>} />
        <Route path="projects" element={<ModuleRoute moduleKey="ProjectManagement"><ProjectListPage /></ModuleRoute>} />
        <Route path="projects/:id" element={<ModuleRoute moduleKey="ProjectManagement"><RouteErrorBoundary><ProjectDetailsPage /></RouteErrorBoundary></ModuleRoute>} />
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
