import { useEffect, useState } from 'react';
import Topbar from '../components/layout/Topbar';
import apiClient from '../services/api';
import type { Inventory } from '../types';

export default function InventoryManagement() {
  const [inventory, setInventory] = useState<Inventory[]>([]);
  const [loading, setLoading] = useState(true);
  const [showLowOnly, setShowLowOnly] = useState(false);

  useEffect(() => { fetchInventory(); }, []);

  const fetchInventory = async () => {
    try {
      const res = await apiClient.get('/inventory');
      setInventory(res.data.data || []);
    } catch { /* fallback */ } finally { setLoading(false); }
  };

  const filtered = showLowOnly ? inventory.filter(i => i.confirmedQuantity <= i.safetyStock) : inventory;

  const stockStatus = (item: Inventory) => {
    if (item.confirmedQuantity <= 0) return { label: '품절', cls: 'rejected' };
    if (item.confirmedQuantity <= item.safetyStock) return { label: '부족', cls: 'pending' };
    return { label: '정상', cls: 'approved' };
  };

  return (
    <div className="flex-1 flex flex-col">
      <Topbar title="재고 현황" breadcrumb="재고 현황" />
      <div className="flex-1 p-7 overflow-y-auto">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-[22px] font-bold text-gray-900 mb-1">재고 현황</h2>
            <p className="text-sm text-gray-500">품목별 재고 수량 및 안전재고 관리</p>
          </div>
          <div className="flex gap-2">
            <button onClick={() => setShowLowOnly(!showLowOnly)} className={`btn-secondary ${showLowOnly ? 'bg-red-50 border-danger text-danger' : ''}`}>
              <i className="fas fa-exclamation-triangle"></i> {showLowOnly ? '전체 보기' : '부족 품목만'}
            </button>
            <button className="btn-primary"><i className="fas fa-sync-alt"></i> 재고 갱신</button>
          </div>
        </div>

        {/* Summary Cards */}
        <div className="grid grid-cols-4 gap-5 mb-6">
          <div className="card p-5">
            <div className="text-sm text-gray-500 mb-1">전체 품목</div>
            <div className="text-2xl font-extrabold text-gray-900">{inventory.length}</div>
          </div>
          <div className="card p-5">
            <div className="text-sm text-gray-500 mb-1">정상 재고</div>
            <div className="text-2xl font-extrabold text-success">{inventory.filter(i => i.confirmedQuantity > i.safetyStock).length}</div>
          </div>
          <div className="card p-5">
            <div className="text-sm text-gray-500 mb-1">재고 부족</div>
            <div className="text-2xl font-extrabold text-warning">{inventory.filter(i => i.confirmedQuantity > 0 && i.confirmedQuantity <= i.safetyStock).length}</div>
          </div>
          <div className="card p-5">
            <div className="text-sm text-gray-500 mb-1">품절</div>
            <div className="text-2xl font-extrabold text-danger">{inventory.filter(i => i.confirmedQuantity <= 0).length}</div>
          </div>
        </div>

        <div className="card overflow-hidden">
          <table className="w-full">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">품목명</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">확정수량</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">입고예정</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">출고예정</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">안전재고</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">상태</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">최종 갱신</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400">로딩 중...</td></tr>
              ) : filtered.length === 0 ? (
                <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400">재고 데이터가 없습니다.</td></tr>
              ) : filtered.map(item => {
                const status = stockStatus(item);
                return (
                  <tr key={item.inventoryId} className="hover:bg-gray-50 transition-colors">
                    <td className="px-5 py-3.5 text-sm font-medium text-gray-700 border-b border-gray-100">{item.product?.productName || `품목 #${item.productId}`}</td>
                    <td className="px-5 py-3.5 text-sm font-semibold text-gray-900 border-b border-gray-100">{item.confirmedQuantity.toLocaleString()}</td>
                    <td className="px-5 py-3.5 text-sm text-primary border-b border-gray-100">+{item.pendingInQuantity.toLocaleString()}</td>
                    <td className="px-5 py-3.5 text-sm text-danger border-b border-gray-100">-{item.pendingOutQuantity.toLocaleString()}</td>
                    <td className="px-5 py-3.5 text-sm text-gray-700 border-b border-gray-100">{item.safetyStock.toLocaleString()}</td>
                    <td className="px-5 py-3.5 text-sm border-b border-gray-100">
                      <span className={`status-badge ${status.cls}`}><span className="w-1.5 h-1.5 rounded-full bg-current"></span>{status.label}</span>
                    </td>
                    <td className="px-5 py-3.5 text-sm text-gray-500 border-b border-gray-100">{new Date(item.lastUpdatedAt).toLocaleDateString('ko-KR')}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
