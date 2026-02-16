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
      <div className="flex-1 p-4 sm:p-6 overflow-y-auto">
        {/* Page Header */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-5">
          <div>
            <h2 className="text-lg font-bold text-gray-900 mb-0.5">발주 관리</h2>
            <p className="text-[13px] text-gray-500">발주 등록, 조회 및 상태 관리</p>
          </div>
          <button className="btn-primary self-start"><i className="fas fa-plus"></i> 신규 발주</button>
        </div>

        {/* Filter */}
        <div className="card p-4 sm:p-5 mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-[13px] font-bold text-gray-900 flex items-center gap-2"><i className="fas fa-filter text-primary text-xs"></i> 검색 필터</h3>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 mb-3">
            <div>
              <label className="form-label">상태</label>
              <select value={filterState} onChange={e => setFilterState(e.target.value)} className="form-input">
                <option value="all">전체</option>
                <option value="0">작성중</option>
                <option value="1">발주완료</option>
                <option value="2">취소</option>
              </select>
            </div>
            <div>
              <label className="form-label">거래처</label>
              <input value={searchKeyword} onChange={e => setSearchKeyword(e.target.value)} placeholder="거래처명 검색" className="form-input" />
            </div>
            <div>
              <label className="form-label">발주일 (시작)</label>
              <input type="date" className="form-input" />
            </div>
            <div>
              <label className="form-label">발주일 (종료)</label>
              <input type="date" className="form-input" />
            </div>
          </div>
          <div className="flex justify-end gap-2">
            <button onClick={() => { setFilterState('all'); setSearchKeyword(''); }} className="px-4 py-2 rounded-lg bg-gray-100 text-gray-600 text-[13px] font-semibold hover:bg-gray-200 transition-colors">초기화</button>
            <button className="px-4 py-2 rounded-lg bg-primary text-white text-[13px] font-semibold shadow-sm hover:bg-primary-light transition-colors">검색</button>
          </div>
        </div>

        {/* Table Header */}
        <div className="flex items-center justify-between mb-2.5">
          <div className="text-[13px] text-gray-600">총 <strong className="text-primary font-bold">{filtered.length}</strong>건</div>
          <div className="flex gap-2">
            <button className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 text-[12px] font-semibold text-gray-600 hover:border-primary hover:text-primary hover:bg-blue-50/50 transition-all"><i className="fas fa-download"></i> <span className="hidden sm:inline">엑셀 다운로드</span></button>
            <button className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 text-[12px] font-semibold text-gray-600 hover:border-primary hover:text-primary hover:bg-blue-50/50 transition-all"><i className="fas fa-print"></i> <span className="hidden sm:inline">인쇄</span></button>
          </div>
        </div>

        {/* Table */}
        <div className="card overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="bg-gray-50">
                  <th className="table-header">발주번호</th>
                  <th className="table-header">거래처</th>
                  <th className="table-header">발주일</th>
                  <th className="table-header">금액</th>
                  <th className="table-header">상태</th>
                  <th className="table-header">관리</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr><td colSpan={6} className="px-5 py-10 text-center text-gray-400 text-[13px]">로딩 중...</td></tr>
                ) : filtered.length === 0 ? (
                  <tr><td colSpan={6} className="px-5 py-10 text-center text-gray-400 text-[13px]">발주 데이터가 없습니다.</td></tr>
                ) : filtered.map(order => (
                  <tr key={order.orderId} className="hover:bg-gray-50/50 transition-colors">
                    <td className="table-cell font-semibold text-primary cursor-pointer hover:underline">PO-{order.orderId.toString().padStart(6, '0')}</td>
                    <td className="table-cell">{order.company?.companyName || order.companyId}</td>
                    <td className="table-cell">{new Date(order.orderDate).toLocaleDateString('ko-KR')}</td>
                    <td className="table-cell font-semibold text-gray-900">₩{order.totalAmount.toLocaleString()}</td>
                    <td className="table-cell">{badge(order.state)}</td>
                    <td className="table-cell">
                      <div className="w-7 h-7 rounded-lg flex items-center justify-center cursor-pointer text-gray-400 hover:bg-gray-100 hover:text-primary transition-colors">
                        <i className="fas fa-ellipsis-v text-xs"></i>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {/* Pagination */}
          <div className="flex items-center justify-between px-5 py-3 border-t border-gray-100">
            <div className="text-[12px] text-gray-500">1-{filtered.length} / 총 {filtered.length}건</div>
            <div className="flex gap-1">
              <button className="w-8 h-8 rounded-lg border border-gray-200 flex items-center justify-center text-gray-400 hover:border-primary hover:text-primary transition-colors"><i className="fas fa-chevron-left text-[10px]"></i></button>
              <button className="w-8 h-8 rounded-lg bg-primary text-white flex items-center justify-center text-[13px] font-semibold">1</button>
              <button className="w-8 h-8 rounded-lg border border-gray-200 flex items-center justify-center text-gray-400 hover:border-primary hover:text-primary transition-colors"><i className="fas fa-chevron-right text-[10px]"></i></button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
