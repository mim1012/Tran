import { useToastStore } from '../../stores/toastStore';

const icons: Record<string, string> = {
  success: 'fa-check-circle',
  error: 'fa-times-circle',
  warning: 'fa-exclamation-triangle',
  info: 'fa-info-circle',
};

const colors: Record<string, { bg: string; border: string; text: string; icon: string }> = {
  success: { bg: '#F0FDF4', border: '#BBF7D0', text: '#166534', icon: '#22C55E' },
  error: { bg: '#FEF2F2', border: '#FECACA', text: '#991B1B', icon: '#EF4444' },
  warning: { bg: '#FFFBEB', border: '#FDE68A', text: '#92400E', icon: '#F59E0B' },
  info: { bg: '#EFF6FF', border: '#BFDBFE', text: '#1E40AF', icon: '#3B82F6' },
};

export default function Toast() {
  const { toasts, removeToast } = useToastStore();

  if (toasts.length === 0) return null;

  return (
    <div
      style={{
        position: 'fixed',
        top: '16px',
        right: '16px',
        zIndex: 9999,
        display: 'flex',
        flexDirection: 'column',
        gap: '8px',
        maxWidth: '380px',
        width: '100%',
      }}
    >
      {toasts.map((toast) => {
        const c = colors[toast.type];
        return (
          <div
            key={toast.id}
            style={{
              display: 'flex',
              alignItems: 'flex-start',
              gap: '10px',
              padding: '12px 16px',
              borderRadius: '10px',
              background: c.bg,
              border: `1px solid ${c.border}`,
              boxShadow: '0 4px 12px rgba(0,0,0,0.1)',
              animation: 'toast-in 0.25s ease-out',
            }}
          >
            <i
              className={`fas ${icons[toast.type]}`}
              style={{ color: c.icon, fontSize: '16px', marginTop: '1px', flexShrink: 0 }}
            />
            <span style={{ flex: 1, fontSize: '13px', fontWeight: 500, color: c.text, lineHeight: 1.5 }}>
              {toast.message}
            </span>
            <button
              onClick={() => removeToast(toast.id)}
              style={{
                background: 'none',
                border: 'none',
                color: c.text,
                opacity: 0.5,
                cursor: 'pointer',
                padding: '0',
                fontSize: '14px',
                lineHeight: 1,
                flexShrink: 0,
              }}
            >
              <i className="fas fa-times" />
            </button>
          </div>
        );
      })}
      <style>{`
        @keyframes toast-in {
          from { opacity: 0; transform: translateX(20px); }
          to { opacity: 1; transform: translateX(0); }
        }
      `}</style>
    </div>
  );
}
