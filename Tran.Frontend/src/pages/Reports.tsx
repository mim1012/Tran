import Topbar from '../components/layout/Topbar';

export default function Reports() {
  const reportItems = [
    { icon: 'fa-chart-line', title: '매출 분석', desc: '월별/분기별 매출 추이 분석', bgClass: 'bg-blue-50', textClass: 'text-primary' },
    { icon: 'fa-chart-pie', title: '구매 분석', desc: '거래처별 구매 현황 분석', bgClass: 'bg-green-50', textClass: 'text-success' },
    { icon: 'fa-chart-bar', title: '재고 분석', desc: '품목별 재고 회전율 분석', bgClass: 'bg-orange-50', textClass: 'text-warning' },
    { icon: 'fa-file-invoice-dollar', title: '손익 분석', desc: '월별 매출/매입 손익 분석', bgClass: 'bg-purple-50', textClass: 'text-purple' },
    { icon: 'fa-users', title: '거래처 분석', desc: '거래처별 거래 실적 분석', bgClass: 'bg-red-50', textClass: 'text-danger' },
    { icon: 'fa-calendar-alt', title: '기간별 리포트', desc: '사용자 정의 기간 리포트', bgClass: 'bg-gray-50', textClass: 'text-gray-600' },
  ];

  return (
    <div className="flex-1 flex flex-col">
      <Topbar title="리포트" breadcrumb="리포트" />
      <div className="flex-1 p-4 sm:p-6 overflow-y-auto">
        <div className="mb-5 sm:mb-6">
          <h2 className="text-base sm:text-lg font-bold text-gray-900 mb-0.5">리포트</h2>
          <p className="text-xs sm:text-[13px] text-gray-500">매출, 구매, 재고 분석 리포트</p>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 sm:gap-5">
          {reportItems.map((item, idx) => (
            <div key={idx} className="card p-4 sm:p-5 cursor-pointer hover:shadow-lg transition-shadow group">
              <div className={`w-10 h-10 rounded-xl ${item.bgClass} ${item.textClass} flex items-center justify-center text-lg mb-3 group-hover:scale-110 transition-transform`}>
                <i className={`fas ${item.icon}`}></i>
              </div>
              <h3 className="text-[14px] font-bold text-gray-900 mb-0.5">{item.title}</h3>
              <p className="text-[13px] text-gray-500">{item.desc}</p>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
