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

const PAGE_SIZE = 10;

export default function PurchaseManagement() {
  const [purchases, setPurchases] = useState<Purchase[]>([]);
  const [loading, setLoading] = useState(true);
  const [filterState, setFilterState] = useState('all');
  const [searchKeyword, setSearchKeyword] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [currentPage, setCurrentPage] = useState(1);

  useEffect(() => { fetchPurchases(); }, []);

  const fetchPurchases = async () => {
    try {
      const res = await apiClient.get('/purchases/company/OWNER');
      setPurchases(res.data.data || []);
    } catch { /* fallback */ } finally { setLoading(false); }
  };

  const filtered = purchases.filter(p => {
    if (filterState !== 'all' && String(p.state) !== filterState) return false;
    if (searchKeyword && !p.company?.companyName?.includes(searchKeyword)) return false;
    return true;
  });

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const paginated = filtered.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const handleReset = () => {
    setFilterState('all');
    setSearchKeyword('');
    setDateFrom('');
    setDateTo('');
    setCurrentPage(1);
  };

  const badge = (state: string | number) => {
    const s = stateLabels[String(state)] || { label: String(state), cls: 'draft' };
    return <span className={`status-badge ${s.cls}`}><span className="w-1.5 h-1.5 rounded-full bg-current" />{s.label}</span>;
  };

  return (
    <div className="flex-1 flex flex-col">
      <Topbar title="입고 관리" breadcrumb="입고 관리" />
      <div className="flex-1 p-6 overflow-y-auto">
        {/* Page Header */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-6">
          <div>
            <h2 className="text-lg font-bold text-gray-900 mb-0.5">입고 관리</h2>
            <p className="text-[13px] text-gray-500">입고 등록, 검수 및 상태 관리</p>
          </div>
          <button className="btn-primary self-start"><i className="fas fa-plus" /> 입고 등록</button>
        </div>

        {/* Filter Card */}
        <div className="card p-5 mb-5">
          <div className="filter-card-header">
            <h3><i className="fas fa-filter" /> 검색 필터</h3>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 mb-4">
            <div>
              <label className="form-label">상태</label>
              <select value={filterState} onChange={e => { setFilterState(e.target.value); setCurrentPage(1); }} className="form-input">
                <option value="all">전체</option>
                <option value="0">입고대기</option>
                <option value="1">부분입고</option>
                <option value="2">입고완료</option>
                <option value="3">취소</option>
              </select>
            </div>
            <div>
              <label className="form-label">거래처</label>
              <input value={searchKeyword} onChange={e => { setSearchKeyword(e.target.value); setCurrentPage(1); }} placeholder="거래처명 검색" className="form-input" />
            </div>
            <div>
              <label className="form-label">입고일 (시작)</label>
              <input type="date" value={dateFrom} onChange={e => setDateFrom(e.target.value)} className="form-input" />
            </div>
            <div>
              <label className="form-label">입고일 (종료)</label>
              <input type="date" value={dateTo} onChange={e => setDateTo(e.target.value)} className="form-input" />
            </div>
          </div>
          <div className="flex justify-end gap-2">
            <button onClick={handleReset} className="btn-reset">초기화</button>
            <button className="btn-search"><i className="fas fa-search" /> 검색</button>
          </div>
        </div>

        {/* Table Count Bar */}
        <div className="table-count-bar">
          <div className="count">총 <strong>{filtered.length}</strong>건</div>
          <div className="actions">
            <button className="btn-util"><i className="fas fa-download" /> 엑셀 다운로드</button>
            <button className="btn-util"><i className="fas fa-print" /> 인쇄</button>
          </div>
        </div>

        {/* Table */}
        <div className="card overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr>
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
                ) : paginated.length === 0 ? (
                  <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400 text-[13px]">입고 데이터가 없습니다.</td></tr>
                ) : paginated.map(p => (
                  <tr key={p.purchaseId}>
                    <td className="table-cell font-semibold text-primary">GR-{p.purchaseId.toString().padStart(6, '0')}</td>
                    <td className="table-cell">{p.orderId ? `PO-${p.orderId.toString().padStart(6, '0')}` : '-'}</td>
                    <td className="table-cell">{p.company?.companyName || p.companyId}</td>
                    <td className="table-cell">{new Date(p.purchaseDate).toLocaleDateString('ko-KR')}</td>
                    <td className="table-cell font-semibold text-gray-900">₩{p.totalAmount.toLocaleString()}</td>
                    <td className="table-cell">{badge(p.state)}</td>
                    <td className="table-cell">
                      <div className="flex gap-1">
                        <button className="w-7 h-7 rounded-lg flex items-center justify-center text-gray-400 hover:bg-blue-50 hover:text-primary transition-colors"><i className="fas fa-eye text-xs" /></button>
                        <button className="w-7 h-7 rounded-lg flex items-center justify-center text-gray-400 hover:bg-green-50 hover:text-success transition-colors"><i className="fas fa-check text-xs" /></button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {/* Pagination */}
          <div className="pagination">
            <div className="info">{(currentPage - 1) * PAGE_SIZE + 1}-{Math.min(currentPage * PAGE_SIZE, filtered.length)} / 총 {filtered.length}건</div>
            <div className="pages">
              <button className="page-btn" onClick={() => setCurrentPage(p => Math.max(1, p - 1))} disabled={currentPage === 1}><i className="fas fa-chevron-left" /></button>
              {Array.from({ length: totalPages }, (_, i) => i + 1).slice(0, 5).map(p => (
                <button key={p} className={`page-btn ${p === currentPage ? 'active' : ''}`} onClick={() => setCurrentPage(p)}>{p}</button>
              ))}
              <button className="page-btn" onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))} disabled={currentPage === totalPages}><i className="fas fa-chevron-right" /></button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
