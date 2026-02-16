import { Link, useLocation } from 'react-router-dom';

interface SidebarProps {
  isOpen: boolean;
  onClose: () => void;
}

const navSections = [
  {
    title: '메인',
    items: [
      { path: '/', icon: 'fa-th-large', label: '대시보드' },
    ],
  },
  {
    title: '거래명세표',
    items: [
      { path: '/documents', icon: 'fa-file-invoice-dollar', label: '거래명세표' },
    ],
  },
  {
    title: '구매 관리',
    items: [
      { path: '/orders', icon: 'fa-file-invoice', label: '발주 관리', badge: 5 },
      { path: '/purchases', icon: 'fa-truck', label: '입고 관리' },
      { path: '/companies', icon: 'fa-handshake', label: '거래처 관리' },
    ],
  },
  {
    title: '판매 관리',
    items: [
      { path: '/quotations', icon: 'fa-file-alt', label: '견적 관리' },
      { path: '/sales', icon: 'fa-shopping-cart', label: '판매 관리' },
    ],
  },
  {
    title: '재고 관리',
    items: [
      { path: '/products', icon: 'fa-boxes-stacked', label: '품목 관리' },
      { path: '/inventory', icon: 'fa-warehouse', label: '재고 현황' },
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
          className="fixed inset-0 bg-black/40 z-40 lg:hidden"
          onClick={onClose}
        />
      )}

      <aside
        className={`
          fixed lg:static inset-y-0 left-0 z-50
          w-60 min-h-screen
          bg-gradient-to-b from-[#2E4A7A] via-[#3B5998] to-[#4A6FA5]
          text-white flex flex-col shadow-xl
          transform transition-transform duration-200 ease-in-out
          ${isOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}
        `}
      >
        {/* Logo */}
        <div className="px-5 py-5 border-b border-white/10 flex items-center gap-3">
          <div className="w-9 h-9 bg-white/20 rounded-lg flex items-center justify-center text-base font-extrabold flex-shrink-0">
            T
          </div>
          <div className="min-w-0">
            <div className="text-lg font-bold tracking-tight leading-tight">Tran ERP</div>
            <div className="text-[10px] opacity-60 leading-tight">Enterprise Resource Planning</div>
          </div>
        </div>

        {/* Navigation */}
        <nav className="flex-1 px-3 py-4 overflow-y-auto">
          {navSections.map((section, idx) => (
            <div key={idx} className="mb-5">
              <div className="text-[11px] font-semibold uppercase tracking-wider opacity-45 px-3 mb-1.5">
                {section.title}
              </div>
              <div className="space-y-0.5">
                {section.items.map((item) => (
                  <Link
                    key={item.path}
                    to={item.path}
                    onClick={onClose}
                    className={`
                      flex items-center gap-2.5 px-3 py-2 rounded-lg text-[13px] font-medium
                      transition-all duration-150
                      ${location.pathname === item.path
                        ? 'bg-white/20 text-white font-semibold shadow-sm'
                        : 'text-white/70 hover:bg-white/10 hover:text-white'
                      }
                    `}
                  >
                    <i className={`fas ${item.icon} w-4 text-center text-[13px]`}></i>
                    <span className="flex-1 truncate">{item.label}</span>
                    {item.badge && (
                      <span className="bg-red-500 text-white text-[10px] font-bold px-1.5 py-0.5 rounded-full min-w-[18px] text-center leading-none">
                        {item.badge}
                      </span>
                    )}
                  </Link>
                ))}
              </div>
            </div>
          ))}
        </nav>

        {/* User Footer */}
        <div className="px-4 py-3.5 border-t border-white/10 flex items-center gap-2.5">
          <div className="w-8 h-8 rounded-full bg-white/20 flex items-center justify-center font-bold text-[13px] flex-shrink-0">
            김
          </div>
          <div className="flex-1 min-w-0">
            <div className="text-[12px] font-semibold leading-tight truncate">김관리</div>
            <div className="text-[10px] opacity-55 leading-tight">시스템 관리자</div>
          </div>
          <i className="fas fa-ellipsis-v opacity-40 cursor-pointer hover:opacity-100 text-xs"></i>
        </div>
      </aside>
    </>
  );
}
