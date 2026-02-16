import { useEffect, useState } from 'react';
import Topbar from '../components/layout/Topbar';
import apiClient from '../services/api';
import type { Sale } from '../types';

const stateLabels: Record<string, { label: string; cls: string }> = {
  '0': { label: '작성중', cls: 'draft' },
  '1': { label: '판매확정', cls: 'approved' },
  '2': { label: '취소', cls: 'rejected' },
};

export default function SaleManagement() {
  const [sales, setSales] = useState<Sale[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => { fetchSales(); }, []);

  const fetchSales = async () => {
    try {
      const res = await apiClient.get('/sales/company/OWNER');
      setSales(res.data.data || []);
    } catch { /* fallback */ } finally { setLoading(false); }
  };

  const badge = (state: string | number) => {
    const s = stateLabels[String(state)] || { label: String(state), cls: 'draft' };
    return <span className={`status-badge ${s.cls}`}><span className="w-1.5 h-1.5 rounded-full bg-current"></span>{s.label}</span>;
  };

  return (
    <div className="flex-1 flex flex-col">
      <Topbar title="판매 관리" breadcrumb="판매 관리" />
      <div className="flex-1 p-4 sm:p-6 overflow-y-auto">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-5">
          <div>
            <h2 className="text-lg font-bold text-gray-900 mb-0.5">판매 관리</h2>
            <p className="text-[13px] text-gray-500">판매 등록, 조회 및 상태 관리</p>
          </div>
          <button className="btn-primary self-start"><i className="fas fa-plus"></i> 판매 등록</button>
        </div>

        <div className="card p-4 sm:p-5 mb-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
            <div>
              <label className="form-label">상태</label>
              <select className="form-input">
                <option value="">전체</option>
                <option value="0">작성중</option>
                <option value="1">판매확정</option>
                <option value="2">취소</option>
              </select>
            </div>
            <div>
              <label className="form-label">거래처</label>
              <input placeholder="거래처명 검색" className="form-input" />
            </div>
            <div>
              <label className="form-label">판매일</label>
              <input type="date" className="form-input" />
            </div>
            <div className="flex items-end">
              <button className="w-full px-4 py-2 rounded-lg bg-primary text-white text-[13px] font-semibold shadow-sm hover:bg-primary-light transition-colors">검색</button>
            </div>
          </div>
        </div>

        <div className="card overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="bg-gray-50">
                  <th className="table-header">판매번호</th>
                  <th className="table-header">거래처</th>
                  <th className="table-header">판매일</th>
                  <th className="table-header">금액</th>
                  <th className="table-header">상태</th>
                  <th className="table-header">관리</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr><td colSpan={6} className="px-5 py-10 text-center text-gray-400 text-[13px]">로딩 중...</td></tr>
                ) : sales.length === 0 ? (
                  <tr><td colSpan={6} className="px-5 py-10 text-center text-gray-400 text-[13px]">판매 데이터가 없습니다.</td></tr>
                ) : sales.map(s => (
                  <tr key={s.saleId} className="hover:bg-gray-50/50 transition-colors">
                    <td className="table-cell font-semibold text-primary">SL-{s.saleId.toString().padStart(6, '0')}</td>
                    <td className="table-cell">{s.company?.companyName || s.companyId}</td>
                    <td className="table-cell">{new Date(s.saleDate).toLocaleDateString('ko-KR')}</td>
                    <td className="table-cell font-semibold text-gray-900">₩{s.totalAmount.toLocaleString()}</td>
                    <td className="table-cell">{badge(s.state)}</td>
                    <td className="table-cell">
                      <div className="flex gap-1">
                        <button className="w-7 h-7 rounded-lg flex items-center justify-center text-gray-400 hover:bg-blue-50 hover:text-primary transition-colors"><i className="fas fa-eye text-xs"></i></button>
                        <button className="w-7 h-7 rounded-lg flex items-center justify-center text-gray-400 hover:bg-blue-50 hover:text-primary transition-colors"><i className="fas fa-edit text-xs"></i></button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
}
