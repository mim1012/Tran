import { useEffect, useState } from 'react';
import Topbar from '../components/layout/Topbar';
import apiClient from '../services/api';
import type { Purchase } from '../types';

const stateLabels: Record<string, { label: string; cls: string }> = {
  '0': { label: '입고대기', cls: 'pending' },
  '1': { label: '부분입고', cls: 'processing' },
  '2': { label: '입고완료', cls: 'approved' },
  '3': { label: '취소', cls: 'rejected' },
};

export default function PurchaseManagement() {
  const [purchases, setPurchases] = useState<Purchase[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => { fetchPurchases(); }, []);

  const fetchPurchases = async () => {
    try {
      const res = await apiClient.get('/purchases/company/OWNER');
      setPurchases(res.data.data || []);
    } catch { /* fallback */ } finally { setLoading(false); }
  };

  const badge = (state: string | number) => {
    const s = stateLabels[String(state)] || { label: String(state), cls: 'draft' };
    return <span className={`status-badge ${s.cls}`}><span className="w-1.5 h-1.5 rounded-full bg-current"></span>{s.label}</span>;
  };

  return (
    <div className="flex-1 flex flex-col">
      <Topbar title="입고 관리" breadcrumb="입고 관리" />
      <div className="flex-1 p-4 sm:p-6 overflow-y-auto">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-5">
          <div>
            <h2 className="text-lg font-bold text-gray-900 mb-0.5">입고 관리</h2>
            <p className="text-[13px] text-gray-500">입고 등록, 검수 및 상태 관리</p>
          </div>
          <button className="btn-primary self-start"><i className="fas fa-plus"></i> 입고 등록</button>
        </div>

        <div className="card p-4 sm:p-5 mb-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
            <div>
              <label className="form-label">상태</label>
              <select className="form-input">
                <option value="">전체</option>
                <option value="0">입고대기</option>
                <option value="1">부분입고</option>
                <option value="2">입고완료</option>
              </select>
            </div>
            <div>
              <label className="form-label">거래처</label>
              <input placeholder="거래처명 검색" className="form-input" />
            </div>
            <div>
              <label className="form-label">입고일</label>
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
                  <th className="table-header">입고번호</th>
                  <th className="table-header">발주번호</th>
                  <th className="table-header">거래처</th>
                  <th className="table-header">입고일</th>
                  <th className="table-header">금액</th>
                  <th className="table-header">상태</th>
                  <th className="table-header">관리</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400 text-[13px]">로딩 중...</td></tr>
                ) : purchases.length === 0 ? (
                  <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400 text-[13px]">입고 데이터가 없습니다.</td></tr>
                ) : purchases.map(p => (
                  <tr key={p.purchaseId} className="hover:bg-gray-50/50 transition-colors">
                    <td className="table-cell font-semibold text-primary">GR-{p.purchaseId.toString().padStart(6, '0')}</td>
                    <td className="table-cell">{p.orderId ? `PO-${p.orderId.toString().padStart(6, '0')}` : '-'}</td>
                    <td className="table-cell">{p.company?.companyName || p.companyId}</td>
                    <td className="table-cell">{new Date(p.purchaseDate).toLocaleDateString('ko-KR')}</td>
                    <td className="table-cell font-semibold text-gray-900">₩{p.totalAmount.toLocaleString()}</td>
                    <td className="table-cell">{badge(p.state)}</td>
                    <td className="table-cell">
                      <div className="flex gap-1">
                        <button className="w-7 h-7 rounded-lg flex items-center justify-center text-gray-400 hover:bg-blue-50 hover:text-primary transition-colors"><i className="fas fa-eye text-xs"></i></button>
                        <button className="w-7 h-7 rounded-lg flex items-center justify-center text-gray-400 hover:bg-green-50 hover:text-success transition-colors"><i className="fas fa-check text-xs"></i></button>
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
