import { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { addToast } from '../utils/toast';
import type { LoginRequest } from '../models/LoginRequest';

const DEFAULT_APP_ID = import.meta.env.VITE_DEFAULT_APPLICATION_ID ?? '00000000-0000-0000-0000-000000000000';

export function Login() {
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const from = (location.state as { from?: { pathname: string } })?.from?.pathname ?? '/';

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    const trimmedEmail = email.trim();
    if (!trimmedEmail || !password) {
      setError('Email and password are required.');
      return;
    }
    setLoading(true);
    try {
      const credentials: LoginRequest = { email: trimmedEmail, password, applicationId: DEFAULT_APP_ID };
      const result = await login(credentials);
      if (result.success) {
        addToast('success', 'Signed in successfully.');
        navigate(from, { replace: true });
      } else {
        setError(result.error ?? 'Login failed.');
      }
    } catch {
      setError('Something went wrong. Please try again.');
      addToast('error', 'Login failed.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center p-6 app-bg">
      <div className="w-full max-w-[400px]">
        <div className="card p-8 shadow-lg">
          <h1 className="text-2xl font-semibold tracking-tight text-gray-900 dark:text-white">Sign in to Bangkok</h1>
          <p className="mt-2 text-sm text-gray-500 dark:text-gray-400">Enter your credentials to continue.</p>

          <form onSubmit={handleSubmit} className="mt-6 space-y-4">
            {error && (
              <div className="alert-error">
                {error}
              </div>
            )}

            <div className="form-group">
              <label htmlFor="email" className="input-label">
                Email
              </label>
              <input
                id="email"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="input"
                placeholder="you@example.com"
                disabled={loading}
              />
            </div>

            <div className="form-group">
              <label htmlFor="password" className="input-label">
                Password
              </label>
              <input
                id="password"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="input"
                placeholder="••••••••"
                disabled={loading}
              />
            </div>

            <button
              type="submit"
              disabled={loading}
              className="btn-primary w-full py-2.5"
            >
              {loading ? 'Signing in…' : 'Sign in'}
            </button>
          </form>
        </div>
        <p className="mt-6 text-center text-sm text-gray-500 dark:text-gray-400">
          Use your Bangkok API credentials. JWT is stored in memory and localStorage.
        </p>
      </div>
    </div>
  );
}
