import Topbar from '../components/layout/Topbar';

export default function Settings() {
  return (
    <div className="flex-1 flex flex-col">
      <Topbar title="설정" breadcrumb="설정" />
      <div className="flex-1 p-4 sm:p-6 overflow-y-auto">
        <div className="mb-5 sm:mb-6">
          <h2 className="text-base sm:text-lg font-bold text-gray-900 mb-0.5">시스템 설정</h2>
          <p className="text-xs sm:text-[13px] text-gray-500">시스템 환경 및 사용자 설정 관리</p>
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-5">
          <div className="card p-4 sm:p-5">
            <h3 className="text-[14px] font-bold text-gray-900 mb-4 flex items-center gap-2"><i className="fas fa-building text-primary text-sm"></i> 회사 정보</h3>
            <div className="space-y-3.5">
              <div>
                <label className="form-label">회사명</label>
                <input defaultValue="Tran 주식회사" className="form-input" />
              </div>
              <div>
                <label className="form-label">사업자번호</label>
                <input defaultValue="123-45-67890" className="form-input" />
              </div>
              <div>
                <label className="form-label">대표자</label>
                <input defaultValue="홍길동" className="form-input" />
              </div>
            </div>
            <button className="btn-primary mt-4 text-[13px]">저장</button>
          </div>
          <div className="card p-4 sm:p-5">
            <h3 className="text-[14px] font-bold text-gray-900 mb-4 flex items-center gap-2"><i className="fas fa-user-cog text-primary text-sm"></i> 사용자 설정</h3>
            <div className="space-y-3.5">
              <div>
                <label className="form-label">표시 언어</label>
                <select className="form-input">
                  <option>한국어</option>
                  <option>English</option>
                </select>
              </div>
              <div>
                <label className="form-label">페이지당 표시 건수</label>
                <select className="form-input">
                  <option>20건</option>
                  <option>50건</option>
                  <option>100건</option>
                </select>
              </div>
              <div>
                <label className="form-label">알림 설정</label>
                <div className="flex items-center gap-4 mt-1">
                  <label className="flex items-center gap-2 text-[13px] text-gray-700 cursor-pointer">
                    <input type="checkbox" defaultChecked className="w-4 h-4 rounded border-gray-300 text-primary focus:ring-primary" />
                    이메일 알림
                  </label>
                  <label className="flex items-center gap-2 text-[13px] text-gray-700 cursor-pointer">
                    <input type="checkbox" defaultChecked className="w-4 h-4 rounded border-gray-300 text-primary focus:ring-primary" />
                    브라우저 알림
                  </label>
                </div>
              </div>
            </div>
            <button className="btn-primary mt-4 text-[13px]">저장</button>
          </div>
        </div>
      </div>
    </div>
  );
}
