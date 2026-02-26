import { useEffect, useState } from 'react';
import { getToasts, removeToast, subscribe, type ToastMessage } from '../utils/toast';

const styles: Record<ToastMessage['type'], string> = {
  success: 'bg-emerald-50 dark:bg-emerald-900/25 border-emerald-200 dark:border-emerald-800/50 text-emerald-800 dark:text-emerald-200',
  error: 'bg-red-50 dark:bg-red-900/25 border-red-200 dark:border-red-800/50 text-red-800 dark:text-red-200',
  info: 'bg-gray-50 dark:bg-gray-800/80 border-gray-200 dark:border-gray-700 text-gray-800 dark:text-gray-200',
};

export function ToastContainer() {
  const [toasts, setToasts] = useState<ToastMessage[]>(getToasts);

  useEffect(() => {
    return subscribe(setToasts);
  }, []);

  if (toasts.length === 0) return null;

  return (
    <div className="fixed bottom-6 right-6 z-50 flex flex-col gap-2 max-w-sm w-full pointer-events-none">
      {toasts.map((t) => (
        <div
          key={t.id}
          className={`pointer-events-auto px-4 py-3 rounded-card border shadow-card transition-all duration-200 ${styles[t.type]}`}
          role="alert"
        >
          <div className="flex items-start justify-between gap-2">
            <p className="text-sm font-medium">{t.message}</p>
            <button
              type="button"
              onClick={() => removeToast(t.id)}
              className="shrink-0 opacity-70 hover:opacity-100 transition-opacity duration-150"
              aria-label="Dismiss"
            >
              <span className="sr-only">Dismiss</span>
              <span aria-hidden>×</span>
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}
