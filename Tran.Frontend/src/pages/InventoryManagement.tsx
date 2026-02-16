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
      <div className="flex-1 p-4 sm:p-6 overflow-y-auto">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-5">
          <div>
            <h2 className="text-lg font-bold text-gray-900 mb-0.5">재고 현황</h2>
            <p className="text-[13px] text-gray-500">품목별 재고 수량 및 안전재고 관리</p>
          </div>
          <div className="flex gap-2 self-start">
            <button onClick={() => setShowLowOnly(!showLowOnly)} className={`btn-secondary ${showLowOnly ? 'bg-red-50 border-danger text-danger' : ''}`}>
              <i className="fas fa-exclamation-triangle"></i> {showLowOnly ? '전체 보기' : '부족 품목만'}
            </button>
            <button className="btn-primary"><i className="fas fa-sync-alt"></i> 재고 갱신</button>
          </div>
        </div>

        {/* Summary Cards */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-5">
          <div className="card p-4">
            <div className="text-[13px] text-gray-500 mb-1">전체 품목</div>
            <div className="text-xl font-extrabold text-gray-900">{inventory.length}</div>
          </div>
          <div className="card p-4">
            <div className="text-[13px] text-gray-500 mb-1">정상 재고</div>
            <div className="text-xl font-extrabold text-success">{inventory.filter(i => i.confirmedQuantity > i.safetyStock).length}</div>
          </div>
          <div className="card p-4">
            <div className="text-[13px] text-gray-500 mb-1">재고 부족</div>
            <div className="text-xl font-extrabold text-warning">{inventory.filter(i => i.confirmedQuantity > 0 && i.confirmedQuantity <= i.safetyStock).length}</div>
          </div>
          <div className="card p-4">
            <div className="text-[13px] text-gray-500 mb-1">품절</div>
            <div className="text-xl font-extrabold text-danger">{inventory.filter(i => i.confirmedQuantity <= 0).length}</div>
          </div>
        </div>

        <div className="card overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="bg-gray-50">
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
                ) : filtered.length === 0 ? (
                  <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400 text-[13px]">재고 데이터가 없습니다.</td></tr>
                ) : filtered.map(item => {
                  const status = stockStatus(item);
                  return (
                    <tr key={item.inventoryId} className="hover:bg-gray-50/50 transition-colors">
                      <td className="table-cell font-medium">{item.product?.productName || `품목 #${item.productId}`}</td>
                      <td className="table-cell font-semibold text-gray-900">{item.confirmedQuantity.toLocaleString()}</td>
                      <td className="table-cell text-primary">+{item.pendingInQuantity.toLocaleString()}</td>
                      <td className="table-cell text-danger">-{item.pendingOutQuantity.toLocaleString()}</td>
                      <td className="table-cell">{item.safetyStock.toLocaleString()}</td>
                      <td className="table-cell">
                        <span className={`status-badge ${status.cls}`}><span className="w-1.5 h-1.5 rounded-full bg-current"></span>{status.label}</span>
                      </td>
                      <td className="table-cell text-gray-500">{new Date(item.lastUpdatedAt).toLocaleDateString('ko-KR')}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
}
