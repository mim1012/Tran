import { useEffect, useState } from 'react';
import Topbar from '../components/layout/Topbar';
import apiClient from '../services/api';
import type { Product } from '../types';

const PAGE_SIZE = 10;

export default function ProductManagement() {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchKeyword, setSearchKeyword] = useState('');
  const [filterCategory, setFilterCategory] = useState('all');
  const [filterActive, setFilterActive] = useState('all');
  const [currentPage, setCurrentPage] = useState(1);

  useEffect(() => { fetchProducts(); }, []);

  const fetchProducts = async () => {
    try {
      const res = await apiClient.get('/products');
      setProducts(res.data.data || []);
    } catch { /* fallback */ } finally { setLoading(false); }
  };

  const filtered = products.filter(p => {
    if (searchKeyword && !p.productName.includes(searchKeyword) && !p.productCode?.includes(searchKeyword)) return false;
    if (filterCategory !== 'all' && p.category !== filterCategory) return false;
    if (filterActive === 'active' && !p.isActive) return false;
    if (filterActive === 'inactive' && p.isActive) return false;
    return true;
  });

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const paginated = filtered.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const handleReset = () => {
    setSearchKeyword('');
    setFilterCategory('all');
    setFilterActive('all');
    setCurrentPage(1);
  };

  return (
    <div className="flex-1 flex flex-col">
      <Topbar title="품목 관리" breadcrumb="품목 관리" />
      <div className="flex-1 p-4 sm:p-6 overflow-y-auto">
        {/* Page Header */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-5 sm:mb-6">
          <div>
            <h2 className="text-base sm:text-lg font-bold text-gray-900 mb-0.5">품목 관리</h2>
            <p className="text-xs sm:text-[13px] text-gray-500">품목 등록, 조회 및 정보 관리</p>
          </div>
          <button className="btn-primary self-start"><i className="fas fa-plus" /> 품목 등록</button>
        </div>

        {/* Filter Card */}
        <div className="card p-4 sm:p-5 mb-4 sm:mb-5">
          <div className="filter-card-header">
            <h3><i className="fas fa-filter" /> 검색 필터</h3>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 mb-4">
            <div>
              <label className="form-label">품목명 / 코드</label>
              <input value={searchKeyword} onChange={e => { setSearchKeyword(e.target.value); setCurrentPage(1); }} placeholder="품목명 또는 코드 검색" className="form-input" />
            </div>
            <div>
              <label className="form-label">카테고리</label>
              <select value={filterCategory} onChange={e => { setFilterCategory(e.target.value); setCurrentPage(1); }} className="form-input">
                <option value="all">전체</option>
                <option value="원자재">원자재</option>
                <option value="부자재">부자재</option>
                <option value="완제품">완제품</option>
              </select>
            </div>
            <div>
              <label className="form-label">상태</label>
              <select value={filterActive} onChange={e => { setFilterActive(e.target.value); setCurrentPage(1); }} className="form-input">
                <option value="all">전체</option>
                <option value="active">활성</option>
                <option value="inactive">비활성</option>
              </select>
            </div>
            <div className="flex items-end justify-end gap-2">
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
            <button className="btn-util"><i className="fas fa-print" /> <span className="hidden sm:inline">인쇄</span></button>
          </div>
        </div>

        {/* Table */}
        <div className="card overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full" style={{ minWidth: '650px' }}>
              <thead>
                <tr>
                  <th className="table-header">품목코드</th>
                  <th className="table-header">품목명</th>
                  <th className="table-header">카테고리</th>
                  <th className="table-header">단위</th>
                  <th className="table-header">기본단가</th>
                  <th className="table-header">상태</th>
                  <th className="table-header">관리</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400 text-[13px]">로딩 중...</td></tr>
                ) : paginated.length === 0 ? (
                  <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400 text-[13px]">품목 데이터가 없습니다.</td></tr>
                ) : paginated.map(product => (
                  <tr key={product.productId}>
                    <td className="table-cell font-semibold text-primary">{product.productCode || `P-${product.productId.toString().padStart(4, '0')}`}</td>
                    <td className="table-cell font-medium">{product.productName}</td>
                    <td className="table-cell">{product.category || '-'}</td>
                    <td className="table-cell">{product.unit}</td>
                    <td className="table-cell font-semibold text-gray-900">₩{product.defaultPrice.toLocaleString()}</td>
                    <td className="table-cell">
                      <span className={`status-badge ${product.isActive ? 'approved' : 'rejected'}`}>
                        <span className="w-1.5 h-1.5 rounded-full bg-current" />
                        {product.isActive ? '활성' : '비활성'}
                      </span>
                    </td>
                    <td className="table-cell">
                      <div className="flex gap-1">
                        <button className="w-7 h-7 rounded-lg flex items-center justify-center text-gray-400 hover:bg-blue-50 hover:text-primary transition-colors"><i className="fas fa-edit text-xs" /></button>
                        <button className="w-7 h-7 rounded-lg flex items-center justify-center text-gray-400 hover:bg-red-50 hover:text-danger transition-colors"><i className="fas fa-trash text-xs" /></button>
                      </div>
                    </td>
                  </tr>
                ))}
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
