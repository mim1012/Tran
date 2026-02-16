import { Link, useLocation } from 'react-router-dom';

interface NavItem {
  path: string;
  icon: string;
  label: string;
  badge?: number;
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

export default function Sidebar() {
  const location = useLocation();

  return (
    <aside className="w-64 min-h-screen bg-gradient-to-b from-[#2E4A7A] via-[#3B5998] to-[#4A6FA5] text-white flex flex-col shadow-2xl">
      {/* Logo */}
      <div className="p-7 pb-5 border-b border-white/10 flex items-center gap-3">
        <div className="w-11 h-11 bg-white/20 rounded-xl flex items-center justify-center text-xl font-extrabold">
          T
        </div>
        <div>
          <div className="text-[22px] font-bold tracking-tight">Tran ERP</div>
          <div className="text-[11px] opacity-65 mt-0.5">Enterprise Resource Planning</div>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 p-4 py-4">
        {navSections.map((section, idx) => (
          <div key={idx} className="mb-6">
            <div className="text-[10px] font-semibold uppercase tracking-wider opacity-50 px-3 mb-2">
              {section.title}
            </div>
            {section.items.map((item) => (
              <Link
                key={item.path}
                to={item.path}
                className={`flex items-center gap-3 px-3.5 py-2.5 rounded-lg text-sm font-medium transition-all mb-0.5 ${
                  location.pathname === item.path
                    ? 'bg-white/20 text-white font-semibold shadow-md'
                    : 'text-white/75 hover:bg-white/10 hover:text-white'
                }`}
              >
                <i className={`fas ${item.icon} w-5 text-center text-[15px]`}></i>
                <span className="flex-1">{item.label}</span>
                {item.badge && (
                  <span className="bg-red-500 text-white text-[11px] font-bold px-2 py-0.5 rounded-full min-w-[22px] text-center">
                    {item.badge}
                  </span>
                )}
              </Link>
            ))}
          </div>
        ))}
      </nav>

      {/* User Footer */}
      <div className="p-4 border-t border-white/10 flex items-center gap-3">
        <div className="w-10 h-10 rounded-full bg-white/20 flex items-center justify-center font-bold text-[15px]">
          김
        </div>
        <div className="flex-1">
          <div className="text-[13px] font-semibold">김관리</div>
          <div className="text-[11px] opacity-60">시스템 관리자</div>
        </div>
        <i className="fas fa-ellipsis-v opacity-50 cursor-pointer hover:opacity-100"></i>
      </div>
    </aside>
  );
}
