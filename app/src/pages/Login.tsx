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
    <div className="min-h-screen flex items-center justify-center p-4 bg-main-area">
      <div className="w-full max-w-md">
        <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 shadow-soft-lg p-8">
          <h1 className="text-2xl font-semibold text-gray-800 dark:text-gray-100 mb-2">Sign in to Bangkok</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mb-6">Enter your credentials to continue.</p>

          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <div className="p-3 rounded-xl bg-red-50 border border-red-100 text-red-700 text-sm">
                {error}
              </div>
            )}

            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                Email
              </label>
              <input
                id="email"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="w-full px-4 py-2.5 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-200 placeholder-gray-400 focus:ring-2 focus:ring-primary-500 focus:border-primary-500 transition-colors"
                placeholder="you@example.com"
                disabled={loading}
              />
            </div>

            <div>
              <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
                Password
              </label>
              <input
                id="password"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full px-4 py-2.5 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-200 placeholder-gray-400 focus:ring-2 focus:ring-primary-500 focus:border-primary-500 transition-colors"
                placeholder="••••••••"
                disabled={loading}
              />
            </div>

            <button
              type="submit"
              disabled={loading}
                className="w-full py-2.5 rounded-lg bg-primary-600 hover:bg-primary-700 text-white font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {loading ? 'Signing in…' : 'Sign in'}
            </button>
          </form>
        </div>
        <p className="mt-4 text-center text-sm text-gray-500 dark:text-gray-400">
          Use your Bangkok API credentials. JWT is stored in memory and localStorage.
        </p>
      </div>
    </div>
  );
}
