import { useEffect, useState } from 'react';
import Topbar from '../components/layout/Topbar';
import apiClient from '../services/api';
import type { Inventory } from '../types';
import { useToastStore } from '../stores/toastStore';

const PAGE_SIZE = 10;

interface InventoryManagementProps {
  embedded?: boolean;
  companyId?: string;
}

export default function InventoryManagement({ embedded, companyId }: InventoryManagementProps) {
  const addToast = useToastStore((s) => s.addToast);
  const [inventory, setInventory] = useState<Inventory[]>([]);
  const [loading, setLoading] = useState(true);
  const [showLowOnly, setShowLowOnly] = useState(false);
  const [searchKeyword, setSearchKeyword] = useState('');
  const [filterStatus, setFilterStatus] = useState('all');
  const [currentPage, setCurrentPage] = useState(1);

  useEffect(() => { fetchInventory(); }, [companyId]);

  const fetchInventory = async () => {
    try {
      const endpoint = companyId ? `/inventory/company/${companyId}` : '/inventory';
      const res = await apiClient.get(endpoint);
      setInventory(res.data.data || []);
    } catch { addToast({ type: 'error', message: '재고 데이터를 불러올 수 없습니다.' }); } finally { setLoading(false); }
  };

  const filtered = inventory.filter(i => {
    if (showLowOnly && i.confirmedQuantity > i.safetyStock) return false;
    if (searchKeyword && !i.product?.productName?.includes(searchKeyword)) return false;
    if (filterStatus === 'normal' && i.confirmedQuantity <= i.safetyStock) return false;
    if (filterStatus === 'low' && (i.confirmedQuantity <= 0 || i.confirmedQuantity > i.safetyStock)) return false;
    if (filterStatus === 'out' && i.confirmedQuantity > 0) return false;
    return true;
  });

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const paginated = filtered.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const handleReset = () => {
    setShowLowOnly(false);
    setSearchKeyword('');
    setFilterStatus('all');
    setCurrentPage(1);
  };

  const stockStatus = (item: Inventory) => {
    if (item.confirmedQuantity <= 0) return { label: '품절', cls: 'rejected' };
    if (item.confirmedQuantity <= item.safetyStock) return { label: '부족', cls: 'pending' };
    return { label: '정상', cls: 'approved' };
  };

  const summaryData = {
    total: inventory.length,
    normal: inventory.filter(i => i.confirmedQuantity > i.safetyStock).length,
    low: inventory.filter(i => i.confirmedQuantity > 0 && i.confirmedQuantity <= i.safetyStock).length,
    out: inventory.filter(i => i.confirmedQuantity <= 0).length,
  };

  return (
    <div className="flex-1 flex flex-col">
      {!embedded && <Topbar title="재고 현황" breadcrumb="재고 현황" />}
      <div className="flex-1 p-4 sm:p-6 overflow-y-auto">
        {/* Page Header */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-5 sm:mb-6">
          <div>
            <h2 className="text-base sm:text-lg font-bold text-gray-900 mb-0.5">재고 현황</h2>
            <p className="text-xs sm:text-[13px] text-gray-500">품목별 재고 수량 및 안전재고 관리</p>
          </div>
          <div className="flex gap-2 self-start">
            <button onClick={() => { setShowLowOnly(!showLowOnly); setCurrentPage(1); }} className="btn-secondary text-xs sm:text-[13px]">
              <i className="fas fa-exclamation-triangle" /> {showLowOnly ? '전체 보기' : '부족 품목만'}
            </button>
            <button className="btn-primary text-xs sm:text-[13px]"><i className="fas fa-sync-alt" /> 재고 갱신</button>
          </div>
        </div>

        {/* Summary Cards */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 sm:gap-5 mb-5 sm:mb-6">
          {[
            { label: '전체 품목', value: summaryData.total, icon: 'fa-boxes-stacked', iconBg: '#EBF0F7', iconColor: '#2E4A7A', valueColor: '#111827' },
            { label: '정상 재고', value: summaryData.normal, icon: 'fa-check-circle', iconBg: '#E8F5E9', iconColor: '#27AE60', valueColor: '#27AE60' },
            { label: '재고 부족', value: summaryData.low, icon: 'fa-exclamation-triangle', iconBg: '#FFF3E0', iconColor: '#F39C12', valueColor: '#F39C12' },
            { label: '품절', value: summaryData.out, icon: 'fa-times-circle', iconBg: '#FFEBEE', iconColor: '#E74C3C', valueColor: '#E74C3C' },
          ].map((card, idx) => (
            <div key={idx} className="card p-4 sm:p-5">
              <div className="flex items-center gap-2.5 sm:gap-3 mb-2.5 sm:mb-3">
                <div className="w-9 h-9 sm:w-10 sm:h-10 rounded-lg sm:rounded-[10px] flex items-center justify-center text-sm sm:text-base" style={{ background: card.iconBg, color: card.iconColor }}>
                  <i className={`fas ${card.icon}`} />
                </div>
                <div className="text-xs sm:text-[13px] text-gray-500 font-medium">{card.label}</div>
              </div>
              <div className="text-xl sm:text-2xl font-extrabold" style={{ color: card.valueColor }}>{card.value}</div>
            </div>
          ))}
        </div>

        {/* Filter Card */}
        <div className="card p-4 sm:p-5 mb-4 sm:mb-5">
          <div className="filter-card-header">
            <h3><i className="fas fa-filter" /> 검색 필터</h3>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 mb-4">
            <div>
              <label className="form-label">품목명</label>
              <input value={searchKeyword} onChange={e => { setSearchKeyword(e.target.value); setCurrentPage(1); }} placeholder="품목명 검색" className="form-input" />
            </div>
            <div>
              <label className="form-label">재고 상태</label>
              <select value={filterStatus} onChange={e => { setFilterStatus(e.target.value); setCurrentPage(1); }} className="form-input">
                <option value="all">전체</option>
                <option value="normal">정상</option>
                <option value="low">부족</option>
                <option value="out">품절</option>
              </select>
            </div>
            <div className="lg:col-span-2 flex items-end justify-end gap-2">
              <button onClick={handleReset} className="btn-reset">초기화</button>
              <button className="btn-search"><i className="fas fa-search" /> 검색</button>
            </div>
          </div>
        </div>

        {/* Table Count Bar */}
        <div className="table-count-bar">
          <div className="count">총 <strong>{filtered.length}</strong>건</div>
          <div className="actions">
            <button className="btn-util"><i className="fas fa-download" /> <span className="hidden sm:inline">엑셀 다운로드</span></button>
          </div>
        </div>

        {/* Table */}
        <div className="card overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full" style={{ minWidth: '650px' }}>
              <thead>
                <tr>
                  <th className="table-header">품목명</th>
                  <th className="table-header">확정수량</th>
                  <th className="table-header">입고예정</th>
                  <th className="table-header">출고예정</th>
                  <th className="table-header">안전재고</th>
                  <th className="table-header">상태</th>
                  <th className="table-header">최종 갱신</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400 text-[13px]">로딩 중...</td></tr>
                ) : paginated.length === 0 ? (
                  <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400 text-[13px]">재고 데이터가 없습니다.</td></tr>
                ) : paginated.map(item => {
                  const status = stockStatus(item);
                  return (
                    <tr key={item.inventoryId}>
                      <td className="table-cell font-medium">{item.product?.productName || `품목 #${item.productId}`}</td>
                      <td className="table-cell font-semibold text-gray-900">{item.confirmedQuantity.toLocaleString()}</td>
                      <td className="table-cell font-medium" style={{ color: '#2E4A7A' }}>+{item.pendingInQuantity.toLocaleString()}</td>
                      <td className="table-cell font-medium" style={{ color: '#E74C3C' }}>-{item.pendingOutQuantity.toLocaleString()}</td>
                      <td className="table-cell">{item.safetyStock.toLocaleString()}</td>
                      <td className="table-cell">
                        <span className={`status-badge ${status.cls}`}><span className="w-1.5 h-1.5 rounded-full bg-current" />{status.label}</span>
                      </td>
                      <td className="table-cell text-gray-500">{new Date(item.lastUpdatedAt).toLocaleDateString('ko-KR')}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
          {/* Pagination */}
          <div className="pagination flex-col sm:flex-row gap-2">
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
