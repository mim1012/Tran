import { useEffect } from 'react';

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: 'danger' | 'default';
  onConfirm: () => void;
  onCancel: () => void;
}

export default function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = '확인',
  cancelLabel = '취소',
  variant = 'default',
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  useEffect(() => {
    if (!open) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onCancel();
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [open, onCancel]);

  if (!open) return null;

  const confirmBg = variant === 'danger' ? '#EF4444' : '#2E4A7A';
  const confirmHoverBg = variant === 'danger' ? '#DC2626' : '#3B5998';

  return (
    <div
      style={{
        position: 'fixed',
        inset: 0,
        zIndex: 9998,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: 'rgba(0,0,0,0.4)',
        padding: '16px',
      }}
      onClick={(e) => {
        if (e.target === e.currentTarget) onCancel();
      }}
    >
      <div
        style={{
          background: '#fff',
          borderRadius: '14px',
          boxShadow: '0 20px 60px rgba(0,0,0,0.15)',
          width: '100%',
          maxWidth: '420px',
          animation: 'confirm-in 0.2s ease-out',
        }}
      >
        <div style={{ padding: '24px 24px 0' }}>
          <h3 style={{ fontSize: '16px', fontWeight: 700, color: '#111827', marginBottom: '8px' }}>
            {title}
          </h3>
          <p style={{ fontSize: '14px', color: '#6B7280', lineHeight: 1.6, whiteSpace: 'pre-line' }}>
            {message}
          </p>
        </div>
        <div
          style={{
            display: 'flex',
            justifyContent: 'flex-end',
            gap: '8px',
            padding: '20px 24px',
          }}
        >
          <button
            onClick={onCancel}
            style={{
              padding: '8px 18px',
              borderRadius: '8px',
              border: '1px solid #E5E7EB',
              background: '#F9FAFB',
              color: '#374151',
              fontSize: '13px',
              fontWeight: 600,
              cursor: 'pointer',
              transition: 'background 0.15s',
            }}
            onMouseEnter={(e) => (e.currentTarget.style.background = '#F3F4F6')}
            onMouseLeave={(e) => (e.currentTarget.style.background = '#F9FAFB')}
          >
            {cancelLabel}
          </button>
          <button
            onClick={onConfirm}
            style={{
              padding: '8px 18px',
              borderRadius: '8px',
              border: 'none',
              background: confirmBg,
              color: '#fff',
              fontSize: '13px',
              fontWeight: 600,
              cursor: 'pointer',
              transition: 'background 0.15s',
            }}
            onMouseEnter={(e) => (e.currentTarget.style.background = confirmHoverBg)}
            onMouseLeave={(e) => (e.currentTarget.style.background = confirmBg)}
          >
            {confirmLabel}
          </button>
        </div>
      </div>
      <style>{`
        @keyframes confirm-in {
          from { opacity: 0; transform: scale(0.95); }
          to { opacity: 1; transform: scale(1); }
        }
      `}</style>
    </div>
  );
}
