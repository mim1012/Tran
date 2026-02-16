interface TopbarProps {
  title: string;
  breadcrumb?: string;
}

export default function Topbar({ title, breadcrumb }: TopbarProps) {
  return (
    <header className="h-16 bg-white px-8 flex items-center justify-between shadow-sm z-10">
      <div className="flex items-center gap-4">
        <div>
          <h1 className="text-xl font-bold text-gray-900">{title}</h1>
          {breadcrumb && (
            <div className="flex items-center gap-1.5 text-xs text-gray-500">
              홈 <i className="fas fa-chevron-right text-[8px]"></i>{' '}
              <span className="text-primary font-medium">{breadcrumb}</span>
            </div>
          )}
        </div>
      </div>

      <div className="flex items-center gap-5">
        {/* Search Box */}
        <div className="flex items-center gap-2 bg-gray-100 rounded-lg px-4 py-2">
          <i className="fas fa-search text-gray-400 text-sm"></i>
          <input
            type="text"
            placeholder="메뉴, 발주번호, 품목 검색..."
            className="bg-transparent border-none outline-none text-sm w-52 text-gray-700"
          />
        </div>

        {/* Notification Icon */}
        <div className="relative w-10 h-10 rounded-lg flex items-center justify-center cursor-pointer text-gray-600 hover:bg-gray-100 transition-colors">
          <i className="far fa-bell text-lg"></i>
          <div className="absolute top-2 right-2 w-2 h-2 bg-red-500 rounded-full border-2 border-white"></div>
        </div>

        {/* Help Icon */}
        <div className="w-10 h-10 rounded-lg flex items-center justify-center cursor-pointer text-gray-600 hover:bg-gray-100 transition-colors">
          <i className="far fa-question-circle text-lg"></i>
        </div>
      </div>
    </header>
  );
}
