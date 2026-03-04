import { Component, type ErrorInfo, StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

class RootErrorBoundary extends Component<{ children: React.ReactNode }, { error: Error | null }> {
  state = { error: null as Error | null }

  static getDerivedStateFromError(error: Error) {
    return { error }
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('RootErrorBoundary:', error, errorInfo)
  }

  render() {
    if (this.state.error) {
      return (
        <div className="min-h-screen p-6 app-bg flex items-center justify-center">
          <div className="max-w-xl w-full rounded-xl border border-red-200 dark:border-red-900/50 bg-red-50/50 dark:bg-red-900/10 p-6 shadow-sm">
            <h1 className="text-lg font-semibold text-red-800 dark:text-red-200 mb-2">Something went wrong</h1>
            <pre className="p-4 rounded-lg bg-gray-100 dark:bg-slate-800 text-gray-800 dark:text-slate-200 text-sm overflow-auto font-mono">
              {this.state.error.message}
            </pre>
            <p className="mt-4 text-sm text-gray-600 dark:text-slate-400">Check the browser console for details.</p>
          </div>
        </div>
      )
    }
    return this.props.children
  }
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <RootErrorBoundary>
      <App />
    </RootErrorBoundary>
  </StrictMode>,
)
