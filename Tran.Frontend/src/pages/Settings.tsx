import Topbar from '../components/layout/Topbar';

export default function Settings() {
  return (
    <div className="flex-1 flex flex-col">
      <Topbar title="설정" breadcrumb="설정" />
      <div className="flex-1 p-7 overflow-y-auto">
        <div className="mb-6">
          <h2 className="text-[22px] font-bold text-gray-900 mb-1">시스템 설정</h2>
          <p className="text-sm text-gray-500">시스템 환경 및 사용자 설정 관리</p>
        </div>
        <div className="grid grid-cols-2 gap-5">
          <div className="card p-6">
            <h3 className="text-base font-bold text-gray-900 mb-4 flex items-center gap-2"><i className="fas fa-building text-primary"></i> 회사 정보</h3>
            <div className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1.5">회사명</label>
                <input defaultValue="Tran 주식회사" className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none" />
              </div>
              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1.5">사업자번호</label>
                <input defaultValue="123-45-67890" className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none" />
              </div>
              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1.5">대표자</label>
                <input defaultValue="홍길동" className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none" />
              </div>
            </div>
            <button className="btn-primary mt-5 text-sm">저장</button>
          </div>
          <div className="card p-6">
            <h3 className="text-base font-bold text-gray-900 mb-4 flex items-center gap-2"><i className="fas fa-user-cog text-primary"></i> 사용자 설정</h3>
            <div className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1.5">표시 언어</label>
                <select className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none">
                  <option>한국어</option>
                  <option>English</option>
                </select>
              </div>
              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1.5">페이지당 표시 건수</label>
                <select className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none">
                  <option>20건</option>
                  <option>50건</option>
                  <option>100건</option>
                </select>
              </div>
              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1.5">알림 설정</label>
                <div className="flex items-center gap-3 mt-1">
                  <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
                    <input type="checkbox" defaultChecked className="w-4 h-4 rounded border-gray-300 text-primary focus:ring-primary" />
                    이메일 알림
                  </label>
                  <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
                    <input type="checkbox" defaultChecked className="w-4 h-4 rounded border-gray-300 text-primary focus:ring-primary" />
                    브라우저 알림
                  </label>
                </div>
              </div>
            </div>
            <button className="btn-primary mt-5 text-sm">저장</button>
          </div>
        </div>
      </div>
    </div>
  );
}
