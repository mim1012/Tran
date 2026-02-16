import { useEffect, useState } from 'react';
import Topbar from '../components/layout/Topbar';
import apiClient from '../services/api';
import type { Order } from '../types';

const stateLabels: Record<string, { label: string; cls: string }> = {
  '0': { label: '작성중', cls: 'draft' },
  '1': { label: '발주완료', cls: 'approved' },
  '2': { label: '취소', cls: 'rejected' },
  Draft: { label: '작성중', cls: 'draft' },
  Completed: { label: '발주완료', cls: 'approved' },
  Cancelled: { label: '취소', cls: 'rejected' },
};

export default function OrderManagement() {
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [filterState, setFilterState] = useState('all');
  const [searchKeyword, setSearchKeyword] = useState('');

  useEffect(() => { fetchOrders(); }, []);

  const fetchOrders = async () => {
    try {
      const res = await apiClient.get('/orders/company/OWNER');
      setOrders(res.data.data || []);
    } catch { /* fallback */ } finally { setLoading(false); }
  };

  const filtered = orders.filter(o => {
    if (filterState !== 'all' && String(o.state) !== filterState) return false;
    if (searchKeyword && !o.company?.companyName?.includes(searchKeyword)) return false;
    return true;
  });

  const badge = (state: string | number) => {
    const s = stateLabels[String(state)] || { label: String(state), cls: 'draft' };
    return <span className={`status-badge ${s.cls}`}><span className="w-1.5 h-1.5 rounded-full bg-current"></span>{s.label}</span>;
  };

  return (
    <div className="flex-1 flex flex-col">
      <Topbar title="발주 관리" breadcrumb="발주 관리" />
      <div className="flex-1 p-7 overflow-y-auto">
        {/* Page Header */}
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-[22px] font-bold text-gray-900 mb-1">발주 관리</h2>
            <p className="text-sm text-gray-500">발주 등록, 조회 및 상태 관리</p>
          </div>
          <button className="btn-primary"><i className="fas fa-plus"></i> 신규 발주</button>
        </div>

        {/* Filter */}
        <div className="card p-6 mb-5">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-base font-bold text-gray-900 flex items-center gap-2"><i className="fas fa-filter text-primary"></i> 검색 필터</h3>
          </div>
          <div className="grid grid-cols-4 gap-4 mb-4">
            <div>
              <label className="block text-xs font-semibold text-gray-600 mb-1.5">상태</label>
              <select value={filterState} onChange={e => setFilterState(e.target.value)} className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none">
                <option value="all">전체</option>
                <option value="0">작성중</option>
                <option value="1">발주완료</option>
                <option value="2">취소</option>
              </select>
            </div>
            <div>
              <label className="block text-xs font-semibold text-gray-600 mb-1.5">거래처</label>
              <input value={searchKeyword} onChange={e => setSearchKeyword(e.target.value)} placeholder="거래처명 검색" className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none" />
            </div>
            <div>
              <label className="block text-xs font-semibold text-gray-600 mb-1.5">발주일 (시작)</label>
              <input type="date" className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none" />
            </div>
            <div>
              <label className="block text-xs font-semibold text-gray-600 mb-1.5">발주일 (종료)</label>
              <input type="date" className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none" />
            </div>
          </div>
          <div className="flex justify-end gap-2">
            <button onClick={() => { setFilterState('all'); setSearchKeyword(''); }} className="px-5 py-2.5 rounded-lg bg-gray-100 text-gray-600 text-sm font-semibold hover:bg-gray-200 transition-colors">초기화</button>
            <button className="px-5 py-2.5 rounded-lg bg-primary text-white text-sm font-semibold shadow hover:bg-primary-light transition-colors">검색</button>
          </div>
        </div>

        {/* Table Header */}
        <div className="flex items-center justify-between mb-3">
          <div className="text-sm text-gray-600">총 <strong className="text-primary font-bold">{filtered.length}</strong>건</div>
          <div className="flex gap-2">
            <button className="flex items-center gap-1.5 px-3.5 py-2 rounded-lg border border-gray-200 text-xs font-semibold text-gray-600 hover:border-primary hover:text-primary hover:bg-blue-50 transition-all"><i className="fas fa-download"></i> 엑셀 다운로드</button>
            <button className="flex items-center gap-1.5 px-3.5 py-2 rounded-lg border border-gray-200 text-xs font-semibold text-gray-600 hover:border-primary hover:text-primary hover:bg-blue-50 transition-all"><i className="fas fa-print"></i> 인쇄</button>
          </div>
        </div>

        {/* Table */}
        <div className="card overflow-hidden">
          <table className="w-full">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">발주번호</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">거래처</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">발주일</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">금액</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">상태</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">관리</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={6} className="px-5 py-10 text-center text-gray-400">로딩 중...</td></tr>
              ) : filtered.length === 0 ? (
                <tr><td colSpan={6} className="px-5 py-10 text-center text-gray-400">발주 데이터가 없습니다.</td></tr>
              ) : filtered.map(order => (
                <tr key={order.orderId} className="hover:bg-gray-50 transition-colors">
                  <td className="px-5 py-3.5 text-sm font-semibold text-primary border-b border-gray-100 cursor-pointer hover:underline">PO-{order.orderId.toString().padStart(6, '0')}</td>
                  <td className="px-5 py-3.5 text-sm text-gray-700 border-b border-gray-100">{order.company?.companyName || order.companyId}</td>
                  <td className="px-5 py-3.5 text-sm text-gray-700 border-b border-gray-100">{new Date(order.orderDate).toLocaleDateString('ko-KR')}</td>
                  <td className="px-5 py-3.5 text-sm font-semibold text-gray-900 border-b border-gray-100">₩{order.totalAmount.toLocaleString()}</td>
                  <td className="px-5 py-3.5 text-sm border-b border-gray-100">{badge(order.state)}</td>
                  <td className="px-5 py-3.5 text-sm border-b border-gray-100">
                    <div className="w-8 h-8 rounded-lg flex items-center justify-center cursor-pointer text-gray-400 hover:bg-gray-100 hover:text-primary transition-colors">
                      <i className="fas fa-ellipsis-v"></i>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {/* Pagination */}
          <div className="flex items-center justify-between px-5 py-4 border-t border-gray-100">
            <div className="text-sm text-gray-500">1-{filtered.length} / 총 {filtered.length}건</div>
            <div className="flex gap-1">
              <button className="w-9 h-9 rounded-lg border border-gray-200 flex items-center justify-center text-gray-400 hover:border-primary hover:text-primary transition-colors"><i className="fas fa-chevron-left text-xs"></i></button>
              <button className="w-9 h-9 rounded-lg bg-primary text-white flex items-center justify-center text-sm font-semibold">1</button>
              <button className="w-9 h-9 rounded-lg border border-gray-200 flex items-center justify-center text-gray-400 hover:border-primary hover:text-primary transition-colors"><i className="fas fa-chevron-right text-xs"></i></button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
