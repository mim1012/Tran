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
      <div className="flex-1 p-7 overflow-y-auto">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-[22px] font-bold text-gray-900 mb-1">입고 관리</h2>
            <p className="text-sm text-gray-500">입고 등록, 검수 및 상태 관리</p>
          </div>
          <button className="btn-primary"><i className="fas fa-plus"></i> 입고 등록</button>
        </div>

        <div className="card p-6 mb-5">
          <div className="grid grid-cols-4 gap-4">
            <div>
              <label className="block text-xs font-semibold text-gray-600 mb-1.5">상태</label>
              <select className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none">
                <option value="">전체</option>
                <option value="0">입고대기</option>
                <option value="1">부분입고</option>
                <option value="2">입고완료</option>
              </select>
            </div>
            <div>
              <label className="block text-xs font-semibold text-gray-600 mb-1.5">거래처</label>
              <input placeholder="거래처명 검색" className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none" />
            </div>
            <div>
              <label className="block text-xs font-semibold text-gray-600 mb-1.5">입고일</label>
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
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">입고번호</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">발주번호</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">거래처</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">입고일</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">금액</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">상태</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">관리</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400">로딩 중...</td></tr>
              ) : purchases.length === 0 ? (
                <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400">입고 데이터가 없습니다.</td></tr>
              ) : purchases.map(p => (
                <tr key={p.purchaseId} className="hover:bg-gray-50 transition-colors">
                  <td className="px-5 py-3.5 text-sm font-semibold text-primary border-b border-gray-100">GR-{p.purchaseId.toString().padStart(6, '0')}</td>
                  <td className="px-5 py-3.5 text-sm text-gray-700 border-b border-gray-100">{p.orderId ? `PO-${p.orderId.toString().padStart(6, '0')}` : '-'}</td>
                  <td className="px-5 py-3.5 text-sm text-gray-700 border-b border-gray-100">{p.company?.companyName || p.companyId}</td>
                  <td className="px-5 py-3.5 text-sm text-gray-700 border-b border-gray-100">{new Date(p.purchaseDate).toLocaleDateString('ko-KR')}</td>
                  <td className="px-5 py-3.5 text-sm font-semibold text-gray-900 border-b border-gray-100">₩{p.totalAmount.toLocaleString()}</td>
                  <td className="px-5 py-3.5 text-sm border-b border-gray-100">{badge(p.state)}</td>
                  <td className="px-5 py-3.5 text-sm border-b border-gray-100">
                    <div className="flex gap-1">
                      <button className="w-8 h-8 rounded-lg flex items-center justify-center text-gray-400 hover:bg-blue-50 hover:text-primary transition-colors"><i className="fas fa-eye text-xs"></i></button>
                      <button className="w-8 h-8 rounded-lg flex items-center justify-center text-gray-400 hover:bg-green-50 hover:text-success transition-colors"><i className="fas fa-check text-xs"></i></button>
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
