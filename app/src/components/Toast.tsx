import { useEffect, useState } from 'react';
import { getToasts, removeToast, subscribe, type ToastMessage } from '../utils/toast';

const styles: Record<ToastMessage['type'], string> = {
  success: 'bg-[#dff6dd] dark:bg-[#0e5b0e]/30 border-[#107c10] text-[#107c10] dark:text-[#92c354]',
  error: 'bg-[#fde7e9] dark:bg-[#a4262c]/20 border-[#a4262c] text-[#a4262c] dark:text-[#f48771]',
  info: 'bg-[#f3f2f1] dark:bg-[#3b3a39] border-[#8a8886] text-[#323130] dark:text-[#f3f2f1]',
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
          className={`pointer-events-auto px-4 py-3 rounded border shadow-card transition-all duration-200 ${styles[t.type]}`}
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
