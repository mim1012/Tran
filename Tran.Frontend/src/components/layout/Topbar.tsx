interface TopbarProps {
  title: string;
  breadcrumb?: string;
}

export default function Topbar({ title, breadcrumb }: TopbarProps) {
  return (
    <header className="h-14 bg-white px-4 sm:px-6 flex items-center justify-between border-b border-gray-100 flex-shrink-0">
      <div className="flex items-center gap-3">
        <div>
          <h1 className="text-base font-bold text-gray-900 leading-tight">{title}</h1>
          {breadcrumb && (
            <div className="flex items-center gap-1.5 text-[11px] text-gray-400 mt-0.5">
              홈 <i className="fas fa-chevron-right text-[7px]"></i>{' '}
              <span className="text-primary font-medium">{breadcrumb}</span>
            </div>
          )}
        </div>
      </div>

      <div className="flex items-center gap-2 sm:gap-3">
        {/* Search Box */}
        <div className="hidden sm:flex items-center gap-2 bg-gray-50 rounded-lg px-3 py-1.5 border border-gray-100">
          <i className="fas fa-search text-gray-400 text-xs"></i>
          <input
            type="text"
            placeholder="검색..."
            className="bg-transparent border-none outline-none text-[13px] w-36 lg:w-48 text-gray-700 placeholder:text-gray-400"
          />
        </div>

        {/* Mobile Search */}
        <button className="sm:hidden w-8 h-8 rounded-lg flex items-center justify-center text-gray-500 hover:bg-gray-100 transition-colors">
          <i className="fas fa-search text-sm"></i>
        </button>

        {/* Notification Icon */}
        <div className="relative w-8 h-8 rounded-lg flex items-center justify-center cursor-pointer text-gray-500 hover:bg-gray-100 transition-colors">
          <i className="far fa-bell text-sm"></i>
          <div className="absolute top-1 right-1 w-2 h-2 bg-red-500 rounded-full border border-white"></div>
        </div>

        {/* Help Icon */}
        <div className="hidden sm:flex w-8 h-8 rounded-lg items-center justify-center cursor-pointer text-gray-500 hover:bg-gray-100 transition-colors">
          <i className="far fa-question-circle text-sm"></i>
        </div>
      </div>
    </header>
  );
}
