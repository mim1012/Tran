import Topbar from '../components/layout/Topbar';

export default function Reports() {
  return (
    <div className="flex-1 flex flex-col">
      <Topbar title="리포트" breadcrumb="리포트" />
      <div className="flex-1 p-7 overflow-y-auto">
        <div className="mb-6">
          <h2 className="text-[22px] font-bold text-gray-900 mb-1">리포트</h2>
          <p className="text-sm text-gray-500">매출, 구매, 재고 분석 리포트</p>
        </div>
        <div className="grid grid-cols-3 gap-5">
          {[
            { icon: 'fa-chart-line', title: '매출 분석', desc: '월별/분기별 매출 추이 분석', color: 'blue' },
            { icon: 'fa-chart-pie', title: '구매 분석', desc: '거래처별 구매 현황 분석', color: 'green' },
            { icon: 'fa-chart-bar', title: '재고 분석', desc: '품목별 재고 회전율 분석', color: 'orange' },
            { icon: 'fa-file-invoice-dollar', title: '손익 분석', desc: '월별 매출/매입 손익 분석', color: 'purple' },
            { icon: 'fa-users', title: '거래처 분석', desc: '거래처별 거래 실적 분석', color: 'red' },
            { icon: 'fa-calendar-alt', title: '기간별 리포트', desc: '사용자 정의 기간 리포트', color: 'gray' },
          ].map((item, idx) => (
            <div key={idx} className="card p-6 cursor-pointer hover:shadow-lg transition-shadow group">
              <div className={`w-12 h-12 rounded-2xl bg-${item.color}-100 text-${item.color}-600 flex items-center justify-center text-xl mb-4 group-hover:scale-110 transition-transform`}>
                <i className={`fas ${item.icon}`}></i>
              </div>
              <h3 className="text-base font-bold text-gray-900 mb-1">{item.title}</h3>
              <p className="text-sm text-gray-500">{item.desc}</p>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
