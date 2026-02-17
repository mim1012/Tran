import { Link, useLocation } from 'react-router-dom';

interface SidebarProps {
  isOpen: boolean;
  onClose: () => void;
}

interface NavItem {
  path: string;
  icon: string;
  label: string;
  badge?: number;
}

interface NavSection {
  title: string;
  items: NavItem[];
}

const navSections: NavSection[] = [
  {
    title: '메인',
    items: [
      { path: '/', icon: 'fa-th-large', label: '대시보드' },
      { path: '/workspace', icon: 'fa-briefcase', label: '작업 공간', badge: 0 },
    ],
  },
  {
    title: '기준 정보 관리',
    items: [
      { path: '/companies', icon: 'fa-handshake', label: '거래처 관리' },
      { path: '/products', icon: 'fa-boxes-stacked', label: '품목 관리' },
    ],
  },
  {
    title: '구매 관리',
    items: [
      { path: '/orders', icon: 'fa-paper-plane', label: '발주 관리', badge: 5 },
      { path: '/purchases', icon: 'fa-truck', label: '입고 관리' },
    ],
  },
  {
    title: '판매 관리',
    items: [
      { path: '/quotations', icon: 'fa-calculator', label: '견적 관리' },
      { path: '/sales', icon: 'fa-shopping-cart', label: '판매 관리' },
    ],
  },
  {
    title: '재고 관리',
    items: [
      { path: '/inventory', icon: 'fa-warehouse', label: '재고 현황' },
    ],
  },
  {
    title: '문서 관리',
    items: [
      { path: '/documents', icon: 'fa-file-invoice-dollar', label: '통합 문서함' },
    ],
  },
  {
    title: '시스템',
    items: [
      { path: '/reports', icon: 'fa-chart-bar', label: '리포트' },
      { path: '/settings', icon: 'fa-cog', label: '설정' },
    ],
  },
];

export default function Sidebar({ isOpen, onClose }: SidebarProps) {
  const location = useLocation();

  return (
    <>
      {/* Mobile overlay */}
      {isOpen && (
        <div
          className="fixed inset-0 z-40 lg:hidden"
          style={{ backgroundColor: 'rgba(0,0,0,0.4)' }}
          onClick={onClose}
        />
      )}

      <aside
        className={`
          fixed lg:static inset-y-0 left-0 z-50
          w-[240px] min-h-screen
          text-white flex flex-col
          transform transition-transform duration-200 ease-in-out
          ${isOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}
        `}
        style={{
          background: 'linear-gradient(to bottom, #2E4A7A 0%, #3B5998 50%, #4A6FA5 100%)',
          boxShadow: '2px 0 12px rgba(46, 74, 122, 0.3)',
        }}
      >
        {/* Logo */}
        <div
          className="flex items-center gap-3"
          style={{
            padding: '20px 20px',
            borderBottom: '1px solid rgba(255,255,255,0.12)',
          }}
        >
          <div
            className="flex items-center justify-center flex-shrink-0"
            style={{
              width: '36px',
              height: '36px',
              background: 'rgba(255,255,255,0.2)',
              borderRadius: '10px',
              fontSize: '15px',
              fontWeight: 800,
            }}
          >
            T
          </div>
          <div className="min-w-0">
            <div style={{ fontSize: '17px', fontWeight: 700, letterSpacing: '-0.02em', lineHeight: 1.2 }}>
              Tran ERP
            </div>
            <div style={{ fontSize: '10px', opacity: 0.55, lineHeight: 1.2 }}>
              Enterprise Resource Planning
            </div>
          </div>
        </div>

        {/* Navigation */}
        <nav
          className="flex-1 overflow-y-auto"
          style={{ padding: '16px 12px' }}
        >
          {navSections.map((section, idx) => (
            <div key={idx} style={{ marginBottom: '20px' }}>
              <div
                style={{
                  fontSize: '11px',
                  fontWeight: 600,
                  textTransform: 'uppercase',
                  letterSpacing: '0.06em',
                  opacity: 0.45,
                  padding: '0 12px',
                  marginBottom: '6px',
                }}
              >
                {section.title}
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '2px' }}>
                {section.items.map((item) => {
                  const isActive = location.pathname === item.path;
                  const isWorkspace = item.path === '/workspace';
                  return (
                    <Link
                      key={item.path}
                      to={item.path}
                      onClick={onClose}
                      className="sidebar-nav-item"
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: '10px',
                        padding: '9px 12px',
                        borderRadius: '8px',
                        fontSize: '13px',
                        fontWeight: isActive ? 600 : 500,
                        color: isActive ? '#ffffff' : 'rgba(255,255,255,0.7)',
                        background: isActive
                          ? isWorkspace
                            ? 'rgba(255,255,255,0.25)'
                            : 'rgba(255,255,255,0.18)'
                          : 'transparent',
                        boxShadow: isActive ? '0 1px 3px rgba(0,0,0,0.1)' : 'none',
                        transition: 'all 0.15s ease',
                        textDecoration: 'none',
                      }}
                    >
                      <i
                        className={`fas ${item.icon}`}
                        style={{ width: '16px', textAlign: 'center', fontSize: '13px' }}
                      />
                      <span style={{ flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                        {item.label}
                      </span>
                      {isWorkspace && (
                        <span
                          style={{
                            background: 'rgba(255,255,255,0.2)',
                            color: '#fff',
                            fontSize: '9px',
                            fontWeight: 700,
                            padding: '2px 6px',
                            borderRadius: '4px',
                            letterSpacing: '0.02em',
                          }}
                        >
                          NEW
                        </span>
                      )}
                      {item.badge !== undefined && item.badge > 0 && !isWorkspace && (
                        <span
                          style={{
                            background: '#EF4444',
                            color: '#fff',
                            fontSize: '10px',
                            fontWeight: 700,
                            padding: '2px 6px',
                            borderRadius: '9999px',
                            minWidth: '18px',
                            textAlign: 'center',
                            lineHeight: 1,
                          }}
                        >
                          {item.badge}
                        </span>
                      )}
                    </Link>
                  );
                })}
              </div>
            </div>
          ))}
        </nav>

        {/* User Footer */}
        <div
          className="flex items-center"
          style={{
            padding: '14px 16px',
            borderTop: '1px solid rgba(255,255,255,0.12)',
            gap: '10px',
          }}
        >
          <div
            className="flex items-center justify-center flex-shrink-0"
            style={{
              width: '32px',
              height: '32px',
              borderRadius: '50%',
              background: 'rgba(255,255,255,0.2)',
              fontWeight: 700,
              fontSize: '13px',
            }}
          >
            김
          </div>
          <div style={{ flex: 1, minWidth: 0 }}>
            <div style={{ fontSize: '12px', fontWeight: 600, lineHeight: 1.2, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
              김관리
            </div>
            <div style={{ fontSize: '10px', opacity: 0.55, lineHeight: 1.2 }}>
              시스템 관리자
            </div>
          </div>
          <i className="fas fa-ellipsis-v" style={{ opacity: 0.4, cursor: 'pointer', fontSize: '12px' }} />
        </div>
      </aside>
    </>
  );
}
