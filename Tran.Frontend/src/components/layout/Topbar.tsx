interface TopbarProps {
  title: string;
  breadcrumb?: string;
}

export default function Topbar({ title, breadcrumb }: TopbarProps) {
  return (
    <header
      style={{
        height: '56px',
        background: '#fff',
        padding: '0 24px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        borderBottom: '1px solid #F3F4F6',
        flexShrink: 0,
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
        <div>
          <h1 style={{ fontSize: '16px', fontWeight: 700, color: '#111827', lineHeight: 1.2, margin: 0 }}>
            {title}
          </h1>
          {breadcrumb && (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '6px',
                fontSize: '11px',
                color: '#9CA3AF',
                marginTop: '2px',
              }}
            >
              홈 <i className="fas fa-chevron-right" style={{ fontSize: '7px' }} />{' '}
              <span style={{ color: '#2E4A7A', fontWeight: 500 }}>{breadcrumb}</span>
            </div>
          )}
        </div>
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
        {/* Search Box */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            background: '#F9FAFB',
            borderRadius: '8px',
            padding: '7px 12px',
            border: '1px solid #F3F4F6',
          }}
        >
          <i className="fas fa-search" style={{ color: '#9CA3AF', fontSize: '12px' }} />
          <input
            type="text"
            placeholder="검색..."
            style={{
              background: 'transparent',
              border: 'none',
              outline: 'none',
              fontSize: '13px',
              width: '160px',
              color: '#374151',
            }}
          />
        </div>

        {/* Notification Icon */}
        <div
          style={{
            position: 'relative',
            width: '34px',
            height: '34px',
            borderRadius: '8px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            cursor: 'pointer',
            color: '#6B7280',
            transition: 'background 0.15s',
          }}
        >
          <i className="far fa-bell" style={{ fontSize: '15px' }} />
          <div
            style={{
              position: 'absolute',
              top: '5px',
              right: '5px',
              width: '8px',
              height: '8px',
              background: '#EF4444',
              borderRadius: '50%',
              border: '2px solid #fff',
            }}
          />
        </div>

        {/* Help Icon */}
        <div
          style={{
            width: '34px',
            height: '34px',
            borderRadius: '8px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            cursor: 'pointer',
            color: '#6B7280',
            transition: 'background 0.15s',
          }}
        >
          <i className="far fa-question-circle" style={{ fontSize: '15px' }} />
        </div>
      </div>
    </header>
  );
}
