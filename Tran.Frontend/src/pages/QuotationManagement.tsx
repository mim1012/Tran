import { useEffect, useState } from 'react';
import Topbar from '../components/layout/Topbar';
import apiClient from '../services/api';
import type { Quotation } from '../types';

const stateLabels: Record<string, { label: string; cls: string }> = {
  '0': { label: '작성중', cls: 'draft' },
  '1': { label: '발송완료', cls: 'processing' },
  '2': { label: '검토중', cls: 'pending' },
  '3': { label: '확정', cls: 'approved' },
  '4': { label: '수정요청', cls: 'pending' },
  '5': { label: '만료', cls: 'rejected' },
};

export default function QuotationManagement() {
  const [quotations, setQuotations] = useState<Quotation[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => { fetchQuotations(); }, []);

  const fetchQuotations = async () => {
    try {
      const res = await apiClient.get('/quotations/company/OWNER');
      setQuotations(res.data.data || []);
    } catch { /* fallback */ } finally { setLoading(false); }
  };

  const badge = (state: string | number) => {
    const s = stateLabels[String(state)] || { label: String(state), cls: 'draft' };
    return <span className={`status-badge ${s.cls}`}><span className="w-1.5 h-1.5 rounded-full bg-current"></span>{s.label}</span>;
  };

  return (
    <div className="flex-1 flex flex-col">
      <Topbar title="견적 관리" breadcrumb="견적 관리" />
      <div className="flex-1 p-7 overflow-y-auto">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-[22px] font-bold text-gray-900 mb-1">견적 관리</h2>
            <p className="text-sm text-gray-500">견적서 작성, 발송 및 상태 관리</p>
          </div>
          <button className="btn-primary"><i className="fas fa-plus"></i> 견적서 작성</button>
        </div>

        <div className="card p-6 mb-5">
          <div className="grid grid-cols-4 gap-4">
            <div>
              <label className="block text-xs font-semibold text-gray-600 mb-1.5">상태</label>
              <select className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none">
                <option value="">전체</option>
                <option value="0">작성중</option>
                <option value="1">발송완료</option>
                <option value="3">확정</option>
                <option value="5">만료</option>
              </select>
            </div>
            <div>
              <label className="block text-xs font-semibold text-gray-600 mb-1.5">거래처</label>
              <input placeholder="거래처명 검색" className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none" />
            </div>
            <div>
              <label className="block text-xs font-semibold text-gray-600 mb-1.5">견적일</label>
              <input type="date" className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none" />
            </div>
            <div className="flex items-end">
              <button className="w-full px-5 py-2.5 rounded-lg bg-primary text-white text-sm font-semibold shadow hover:bg-primary-light transition-colors">검색</button>
            </div>
          </div>
        </div>

        <div className="card overflow-hidden">
          <table className="w-full">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">견적번호</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">거래처</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">견적일</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">유효기간</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">금액</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">상태</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">관리</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400">로딩 중...</td></tr>
              ) : quotations.length === 0 ? (
                <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400">견적 데이터가 없습니다.</td></tr>
              ) : quotations.map(q => (
                <tr key={q.quotationId} className="hover:bg-gray-50 transition-colors">
                  <td className="px-5 py-3.5 text-sm font-semibold text-primary border-b border-gray-100">QT-{q.quotationId.toString().padStart(6, '0')}</td>
                  <td className="px-5 py-3.5 text-sm text-gray-700 border-b border-gray-100">{q.company?.companyName || q.companyId}</td>
                  <td className="px-5 py-3.5 text-sm text-gray-700 border-b border-gray-100">{new Date(q.quotationDate).toLocaleDateString('ko-KR')}</td>
                  <td className="px-5 py-3.5 text-sm text-gray-700 border-b border-gray-100">{q.validUntil ? new Date(q.validUntil).toLocaleDateString('ko-KR') : '-'}</td>
                  <td className="px-5 py-3.5 text-sm font-semibold text-gray-900 border-b border-gray-100">₩{q.totalAmount.toLocaleString()}</td>
                  <td className="px-5 py-3.5 text-sm border-b border-gray-100">{badge(q.state)}</td>
                  <td className="px-5 py-3.5 text-sm border-b border-gray-100">
                    <div className="flex gap-1">
                      <button className="w-8 h-8 rounded-lg flex items-center justify-center text-gray-400 hover:bg-blue-50 hover:text-primary transition-colors"><i className="fas fa-eye text-xs"></i></button>
                      <button className="w-8 h-8 rounded-lg flex items-center justify-center text-gray-400 hover:bg-blue-50 hover:text-primary transition-colors"><i className="fas fa-edit text-xs"></i></button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
