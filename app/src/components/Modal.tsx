interface ModalProps {
  open: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
}

export function Modal({ open, onClose, title, children }: ModalProps) {
  if (!open) return null;
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/40 backdrop-blur-[2px] transition-opacity duration-200" onClick={onClose} aria-hidden />
      <div
        className="relative w-full max-w-md rounded shadow-modal transition-all duration-200"
        style={{ backgroundColor: 'var(--dropdown-bg, #ffffff)', border: '1px solid var(--dropdown-border, #edebe9)' }}
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-title"
      >
        <div className="flex items-center justify-between border-b px-6 py-4" style={{ borderColor: 'var(--dropdown-border, #edebe9)' }}>
          <h2 id="modal-title" className="text-lg font-semibold" style={{ color: 'var(--dropdown-text, #323130)' }}>
            {title}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="btn-icon"
            aria-label="Close"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
        <div className="px-6 py-5">{children}</div>
      </div>
    </div>
  );
}
