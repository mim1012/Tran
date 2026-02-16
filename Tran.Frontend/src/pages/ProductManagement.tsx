import { useEffect, useState } from 'react';
import Topbar from '../components/layout/Topbar';
import apiClient from '../services/api';
import type { Product } from '../types';

export default function ProductManagement() {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchKeyword, setSearchKeyword] = useState('');

  useEffect(() => { fetchProducts(); }, []);

  const fetchProducts = async () => {
    try {
      const res = await apiClient.get('/products');
      setProducts(res.data.data || []);
    } catch { /* fallback */ } finally { setLoading(false); }
  };

  const filtered = products.filter(p => {
    if (searchKeyword && !p.productName.includes(searchKeyword) && !p.productCode?.includes(searchKeyword)) return false;
    return true;
  });

  return (
    <div className="flex-1 flex flex-col">
      <Topbar title="품목 관리" breadcrumb="품목 관리" />
      <div className="flex-1 p-4 sm:p-6 overflow-y-auto">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-5">
          <div>
            <h2 className="text-lg font-bold text-gray-900 mb-0.5">품목 관리</h2>
            <p className="text-[13px] text-gray-500">품목 등록, 조회 및 정보 관리</p>
          </div>
          <button className="btn-primary self-start"><i className="fas fa-plus"></i> 품목 등록</button>
        </div>

        <div className="card p-4 sm:p-5 mb-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
            <div>
              <label className="form-label">품목명 / 코드</label>
              <input value={searchKeyword} onChange={e => setSearchKeyword(e.target.value)} placeholder="품목명 또는 코드 검색" className="form-input" />
            </div>
            <div>
              <label className="form-label">카테고리</label>
              <select className="form-input">
                <option value="">전체</option>
                <option value="원자재">원자재</option>
                <option value="부자재">부자재</option>
                <option value="완제품">완제품</option>
              </select>
            </div>
            <div>
              <label className="form-label">상태</label>
              <select className="form-input">
                <option value="">전체</option>
                <option value="active">활성</option>
                <option value="inactive">비활성</option>
              </select>
            </div>
            <div className="flex items-end">
              <button className="w-full px-4 py-2 rounded-lg bg-primary text-white text-[13px] font-semibold shadow-sm hover:bg-primary-light transition-colors">검색</button>
            </div>
          </div>
        </div>

        <div className="flex items-center justify-between mb-2.5">
          <div className="text-[13px] text-gray-600">총 <strong className="text-primary font-bold">{filtered.length}</strong>건</div>
          <div className="flex gap-2">
            <button className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-200 text-[12px] font-semibold text-gray-600 hover:border-primary hover:text-primary hover:bg-blue-50/50 transition-all"><i className="fas fa-download"></i> <span className="hidden sm:inline">엑셀 다운로드</span></button>
          </div>
        </div>

        <div className="card overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="bg-gray-50">
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
                ) : filtered.length === 0 ? (
                  <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400 text-[13px]">품목 데이터가 없습니다.</td></tr>
                ) : filtered.map(product => (
                  <tr key={product.productId} className="hover:bg-gray-50/50 transition-colors">
                    <td className="table-cell font-semibold text-primary">{product.productCode || `P-${product.productId.toString().padStart(4, '0')}`}</td>
                    <td className="table-cell font-medium">{product.productName}</td>
                    <td className="table-cell">{product.category || '-'}</td>
                    <td className="table-cell">{product.unit}</td>
                    <td className="table-cell font-semibold text-gray-900">₩{product.defaultPrice.toLocaleString()}</td>
                    <td className="table-cell">
                      <span className={`status-badge ${product.isActive ? 'approved' : 'rejected'}`}>
                        <span className="w-1.5 h-1.5 rounded-full bg-current"></span>
                        {product.isActive ? '활성' : '비활성'}
                      </span>
                    </td>
                    <td className="table-cell">
                      <div className="flex gap-1">
                        <button className="w-7 h-7 rounded-lg flex items-center justify-center text-gray-400 hover:bg-blue-50 hover:text-primary transition-colors"><i className="fas fa-edit text-xs"></i></button>
                        <button className="w-7 h-7 rounded-lg flex items-center justify-center text-gray-400 hover:bg-red-50 hover:text-danger transition-colors"><i className="fas fa-trash text-xs"></i></button>
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
