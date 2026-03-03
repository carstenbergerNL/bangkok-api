interface FormSidebarProps {
  open: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
  /** Kept for API compatibility; all right sidebars use the same width. */
  width?: 'default' | 'wide';
}

const borderColor = 'var(--sidebar-border, #edebe9)';
const headerColor = 'var(--card-header-color, #323130)';

/** Same width as task drawer: 28rem, max 90vw on small screens */
const RIGHT_SIDEBAR_WIDTH = 'w-[28rem] max-w-[90vw]';

export function FormSidebar({ open, onClose, title, children }: FormSidebarProps) {
  if (!open) return null;

  return (
    <aside
      className={`fixed top-12 right-0 z-40 h-[calc(100vh-3rem)] flex flex-col shrink-0 border-l transition-[width] duration-200 ${RIGHT_SIDEBAR_WIDTH}`}
      style={{
        backgroundColor: 'var(--sidebar-bg, #faf9f8)',
        borderLeft: `1px solid ${borderColor}`,
      }}
      role="dialog"
      aria-modal="true"
      aria-labelledby="form-sidebar-title"
    >
      <div className="flex items-center justify-between border-b px-6 py-4 shrink-0" style={{ borderColor }}>
        <h2 id="form-sidebar-title" className="text-lg font-semibold" style={{ color: headerColor }}>
          {title}
        </h2>
        <button
          type="button"
          onClick={onClose}
          className="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-slate-700 text-gray-500 dark:text-slate-400 transition-colors"
          aria-label="Close"
        >
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>
      <div className="flex-1 overflow-y-auto px-6 py-5">{children}</div>
    </aside>
  );
}
