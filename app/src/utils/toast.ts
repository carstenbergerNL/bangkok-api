export type ToastType = 'success' | 'error' | 'info';

export interface ToastMessage {
  id: string;
  type: ToastType;
  message: string;
}

type Listener = (toasts: ToastMessage[]) => void;

let toasts: ToastMessage[] = [];
const listeners: Set<Listener> = new Set();

function notify() {
  listeners.forEach((fn) => fn([...toasts]));
}

export function addToast(type: ToastType, message: string): void {
  const id = crypto.randomUUID();
  toasts = [...toasts.slice(-4), { id, type, message }];
  notify();
  setTimeout(() => {
    toasts = toasts.filter((t) => t.id !== id);
    notify();
  }, 4000);
}

export function removeToast(id: string): void {
  toasts = toasts.filter((t) => t.id !== id);
  notify();
}

export function subscribe(listener: Listener): () => void {
  listeners.add(listener);
  listener([...toasts]);
  return () => listeners.delete(listener);
}

export function getToasts(): ToastMessage[] {
  return [...toasts];
}
