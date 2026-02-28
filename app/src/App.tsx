import { useEffect } from 'react';
import { BrowserRouter, Navigate, Route, Routes, useNavigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './context/AuthContext';
import { PrivateRoute } from './components/PrivateRoute';
import { AdminRoute } from './components/AdminRoute';
import { ToastContainer } from './components/Toast';
import { MainLayout } from './layouts/MainLayout';
import { Login } from './pages/Login';
import { Dashboard } from './pages/Dashboard';
import { Profile } from './pages/Profile';
import { Roles } from './pages/Roles';
import { AdminSettings } from './pages/AdminSettings';
import { initAuthUnauthorizedHandler } from './services/authService';

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
        <Route path="roles" element={<AdminRoute><Roles /></AdminRoute>} />
        <Route path="admin-settings" element={<AdminRoute><AdminSettings /></AdminRoute>} />
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
