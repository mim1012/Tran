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
      <div className="flex-1 p-7 overflow-y-auto">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-[22px] font-bold text-gray-900 mb-1">품목 관리</h2>
            <p className="text-sm text-gray-500">품목 등록, 조회 및 정보 관리</p>
          </div>
          <button className="btn-primary"><i className="fas fa-plus"></i> 품목 등록</button>
        </div>

        <div className="card p-6 mb-5">
          <div className="grid grid-cols-4 gap-4 mb-4">
            <div>
              <label className="block text-xs font-semibold text-gray-600 mb-1.5">품목명 / 코드</label>
              <input value={searchKeyword} onChange={e => setSearchKeyword(e.target.value)} placeholder="품목명 또는 코드 검색" className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none" />
            </div>
            <div>
              <label className="block text-xs font-semibold text-gray-600 mb-1.5">카테고리</label>
              <select className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none">
                <option value="">전체</option>
                <option value="원자재">원자재</option>
                <option value="부자재">부자재</option>
                <option value="완제품">완제품</option>
              </select>
            </div>
            <div>
              <label className="block text-xs font-semibold text-gray-600 mb-1.5">상태</label>
              <select className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none">
                <option value="">전체</option>
                <option value="active">활성</option>
                <option value="inactive">비활성</option>
              </select>
            </div>
            <div className="flex items-end">
              <button className="w-full px-5 py-2.5 rounded-lg bg-primary text-white text-sm font-semibold shadow hover:bg-primary-light transition-colors">검색</button>
            </div>
          </div>
        </div>

        <div className="flex items-center justify-between mb-3">
          <div className="text-sm text-gray-600">총 <strong className="text-primary font-bold">{filtered.length}</strong>건</div>
          <div className="flex gap-2">
            <button className="flex items-center gap-1.5 px-3.5 py-2 rounded-lg border border-gray-200 text-xs font-semibold text-gray-600 hover:border-primary hover:text-primary hover:bg-blue-50 transition-all"><i className="fas fa-download"></i> 엑셀 다운로드</button>
          </div>
        </div>

        <div className="card overflow-hidden">
          <table className="w-full">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">품목코드</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">품목명</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">카테고리</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">단위</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">기본단가</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">상태</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">관리</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400">로딩 중...</td></tr>
              ) : filtered.length === 0 ? (
                <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400">품목 데이터가 없습니다.</td></tr>
              ) : filtered.map(product => (
                <tr key={product.productId} className="hover:bg-gray-50 transition-colors">
                  <td className="px-5 py-3.5 text-sm font-semibold text-primary border-b border-gray-100">{product.productCode || `P-${product.productId.toString().padStart(4, '0')}`}</td>
                  <td className="px-5 py-3.5 text-sm text-gray-700 border-b border-gray-100 font-medium">{product.productName}</td>
                  <td className="px-5 py-3.5 text-sm text-gray-700 border-b border-gray-100">{product.category || '-'}</td>
                  <td className="px-5 py-3.5 text-sm text-gray-700 border-b border-gray-100">{product.unit}</td>
                  <td className="px-5 py-3.5 text-sm font-semibold text-gray-900 border-b border-gray-100">₩{product.defaultPrice.toLocaleString()}</td>
                  <td className="px-5 py-3.5 text-sm border-b border-gray-100">
                    <span className={`status-badge ${product.isActive ? 'approved' : 'rejected'}`}>
                      <span className="w-1.5 h-1.5 rounded-full bg-current"></span>
                      {product.isActive ? '활성' : '비활성'}
                    </span>
                  </td>
                  <td className="px-5 py-3.5 text-sm border-b border-gray-100">
                    <div className="flex gap-1">
                      <button className="w-8 h-8 rounded-lg flex items-center justify-center text-gray-400 hover:bg-blue-50 hover:text-primary transition-colors"><i className="fas fa-edit text-xs"></i></button>
                      <button className="w-8 h-8 rounded-lg flex items-center justify-center text-gray-400 hover:bg-red-50 hover:text-danger transition-colors"><i className="fas fa-trash text-xs"></i></button>
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
