import { Component, type ErrorInfo, type ReactNode } from 'react';
import { Link } from 'react-router-dom';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class RouteErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false, error: null };

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    this.props.onError?.(error, errorInfo);
    if (typeof console !== 'undefined' && console.error) {
      console.error('RouteErrorBoundary caught an error:', error, errorInfo);
    }
  }

  render() {
    if (this.state.hasError && this.state.error) {
      if (this.props.fallback) return this.props.fallback;
      return (
        <div className="p-6 space-y-4">
          <div className="rounded-xl border border-red-200 dark:border-red-900/50 bg-red-50/50 dark:bg-red-900/10 p-6">
            <h2 className="text-lg font-semibold text-red-800 dark:text-red-200">Something went wrong</h2>
            <p className="mt-1 text-sm text-red-700 dark:text-red-300">{this.state.error.message}</p>
            <Link to="/projects" className="mt-4 inline-block text-sm font-medium text-blue-600 dark:text-blue-400 hover:underline">
              Back to projects
            </Link>
          </div>
        </div>
      );
    }
    return this.props.children;
  }
}
