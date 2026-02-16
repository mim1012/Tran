import { useEffect, useState } from 'react';
import Topbar from '../components/layout/Topbar';
import apiClient from '../services/api';
import type { DashboardSummary } from '../types';

export default function Dashboard() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchDashboardData();
  }, []);

  const fetchDashboardData = async () => {
    try {
      const response = await apiClient.get<{ data: DashboardSummary }>('/dashboard/summary');
      setSummary(response.data.data);
    } catch (error) {
      console.error('Failed to fetch dashboard data:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="flex-1 flex flex-col">
        <Topbar title="대시보드" breadcrumb="대시보드" />
        <div className="flex-1 flex items-center justify-center">
          <div className="text-gray-400 text-sm">로딩 중...</div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex-1 flex flex-col">
      <Topbar title="대시보드" breadcrumb="대시보드" />

      <div className="flex-1 p-4 sm:p-6 overflow-y-auto">
        {/* Summary Cards */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
          <div className="card p-5 relative overflow-hidden">
            <div className="absolute top-0 right-0 w-20 h-20 bg-primary opacity-5 rounded-full -mr-4 -mt-4"></div>
            <div className="w-10 h-10 rounded-xl bg-blue-50 text-primary flex items-center justify-center text-lg mb-3">
              <i className="fas fa-file-invoice"></i>
            </div>
            <div className="text-[13px] text-gray-500 font-medium mb-1">이번 달 발주</div>
            <div className="text-2xl font-extrabold text-gray-900 tracking-tight">{summary?.totalOrders || 0}</div>
            <div className="text-[11px] text-success mt-1.5 flex items-center gap-1">
              <i className="fas fa-arrow-up text-[9px]"></i> 12.5% 전월 대비
            </div>
          </div>

          <div className="card p-5 relative overflow-hidden">
            <div className="absolute top-0 right-0 w-20 h-20 bg-success opacity-5 rounded-full -mr-4 -mt-4"></div>
            <div className="w-10 h-10 rounded-xl bg-green-50 text-success flex items-center justify-center text-lg mb-3">
              <i className="fas fa-check-circle"></i>
            </div>
            <div className="text-[13px] text-gray-500 font-medium mb-1">승인 완료</div>
            <div className="text-2xl font-extrabold text-gray-900 tracking-tight">{summary?.approvedOrders || 0}</div>
            <div className="text-[11px] text-success mt-1.5 flex items-center gap-1">
              <i className="fas fa-arrow-up text-[9px]"></i> 8.3% 전월 대비
            </div>
          </div>

          <div className="card p-5 relative overflow-hidden">
            <div className="absolute top-0 right-0 w-20 h-20 bg-warning opacity-5 rounded-full -mr-4 -mt-4"></div>
            <div className="w-10 h-10 rounded-xl bg-orange-50 text-warning flex items-center justify-center text-lg mb-3">
              <i className="fas fa-clock"></i>
            </div>
            <div className="text-[13px] text-gray-500 font-medium mb-1">승인 대기</div>
            <div className="text-2xl font-extrabold text-gray-900 tracking-tight">{summary?.pendingOrders || 0}</div>
            <div className="text-[11px] text-danger mt-1.5 flex items-center gap-1">
              <i className="fas fa-arrow-down text-[9px]"></i> 5.2% 전월 대비
            </div>
          </div>

          <div className="card p-5 relative overflow-hidden">
            <div className="absolute top-0 right-0 w-20 h-20 bg-danger opacity-5 rounded-full -mr-4 -mt-4"></div>
            <div className="w-10 h-10 rounded-xl bg-red-50 text-danger flex items-center justify-center text-lg mb-3">
              <i className="fas fa-exclamation-triangle"></i>
            </div>
            <div className="text-[13px] text-gray-500 font-medium mb-1">재고 부족 품목</div>
            <div className="text-2xl font-extrabold text-gray-900 tracking-tight">{summary?.lowStockItems || 0}</div>
            <div className="text-[11px] text-danger mt-1.5 flex items-center gap-1">
              <i className="fas fa-arrow-up text-[9px]"></i> 2건 증가
            </div>
          </div>
        </div>

        {/* Recent Orders Table */}
        <div className="mb-6">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-[15px] font-bold text-gray-900">최근 발주 내역</h2>
            <div className="text-[13px] text-primary font-semibold cursor-pointer flex items-center gap-1 hover:underline">
              전체보기 <i className="fas fa-chevron-right text-[9px]"></i>
            </div>
          </div>

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
                  </tr>
                </thead>
                <tbody>
                  {summary?.recentOrders.map((order) => (
                    <tr key={order.orderId} className="hover:bg-gray-50/50 transition-colors">
                      <td className="table-cell font-semibold text-primary">
                        PO-{order.orderId.toString().padStart(6, '0')}
                      </td>
                      <td className="table-cell">{order.companyName}</td>
                      <td className="table-cell">
                        {new Date(order.orderDate).toLocaleDateString('ko-KR')}
                      </td>
                      <td className="table-cell font-semibold text-gray-900">
                        ₩{order.totalAmount.toLocaleString()}
                      </td>
                      <td className="table-cell">
                        <span className={`status-badge ${order.state.toLowerCase()}`}>
                          {order.state === 'Completed' ? '승인완료' : order.state === 'Draft' ? '작성중' : '취소'}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>

        {/* Bottom Grid */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <div className="card p-5 lg:col-span-2">
            <h3 className="text-[14px] font-bold text-gray-900 mb-3">빠른 실행</h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
              <div className="flex items-center gap-3 p-3 rounded-lg border border-gray-100 cursor-pointer hover:border-primary hover:bg-blue-50/50 transition-all">
                <div className="w-9 h-9 rounded-lg bg-blue-50 text-primary flex items-center justify-center text-sm flex-shrink-0">
                  <i className="fas fa-plus"></i>
                </div>
                <div className="text-[13px] font-semibold text-gray-700">신규 발주 등록</div>
              </div>
              <div className="flex items-center gap-3 p-3 rounded-lg border border-gray-100 cursor-pointer hover:border-success hover:bg-green-50/50 transition-all">
                <div className="w-9 h-9 rounded-lg bg-green-50 text-success flex items-center justify-center text-sm flex-shrink-0">
                  <i className="fas fa-box"></i>
                </div>
                <div className="text-[13px] font-semibold text-gray-700">입고 처리</div>
              </div>
              <div className="flex items-center gap-3 p-3 rounded-lg border border-gray-100 cursor-pointer hover:border-warning hover:bg-orange-50/50 transition-all">
                <div className="w-9 h-9 rounded-lg bg-orange-50 text-warning flex items-center justify-center text-sm flex-shrink-0">
                  <i className="fas fa-clipboard-list"></i>
                </div>
                <div className="text-[13px] font-semibold text-gray-700">재고 실사</div>
              </div>
              <div className="flex items-center gap-3 p-3 rounded-lg border border-gray-100 cursor-pointer hover:border-purple hover:bg-purple-50/50 transition-all">
                <div className="w-9 h-9 rounded-lg bg-purple-50 text-purple flex items-center justify-center text-sm flex-shrink-0">
                  <i className="fas fa-file-export"></i>
                </div>
                <div className="text-[13px] font-semibold text-gray-700">리포트 생성</div>
              </div>
            </div>
          </div>

          <div className="card p-5">
            <h3 className="text-[14px] font-bold text-gray-900 mb-3">공지사항</h3>
            <div className="space-y-2.5">
              <div className="flex gap-2.5 pb-2.5 border-b border-gray-50">
                <div className="w-1.5 h-1.5 rounded-full bg-primary mt-1.5 flex-shrink-0"></div>
                <div className="flex-1 min-w-0">
                  <div className="text-[13px] font-semibold text-gray-800 mb-0.5 truncate">2월 정기 재고 실사 안내</div>
                  <div className="text-[11px] text-gray-400">2026-02-14</div>
                </div>
              </div>
              <div className="flex gap-2.5 pb-2.5 border-b border-gray-50">
                <div className="w-1.5 h-1.5 rounded-full bg-warning mt-1.5 flex-shrink-0"></div>
                <div className="flex-1 min-w-0">
                  <div className="text-[13px] font-semibold text-gray-800 mb-0.5 truncate">시스템 점검 예정 (2/20)</div>
                  <div className="text-[11px] text-gray-400">2026-02-13</div>
                </div>
              </div>
              <div className="flex gap-2.5 pb-2.5 border-b border-gray-50">
                <div className="w-1.5 h-1.5 rounded-full bg-success mt-1.5 flex-shrink-0"></div>
                <div className="flex-1 min-w-0">
                  <div className="text-[13px] font-semibold text-gray-800 mb-0.5 truncate">신규 거래처 등록 완료</div>
                  <div className="text-[11px] text-gray-400">2026-02-10</div>
                </div>
              </div>
              <div className="flex gap-2.5">
                <div className="w-1.5 h-1.5 rounded-full bg-gray-300 mt-1.5 flex-shrink-0"></div>
                <div className="flex-1 min-w-0">
                  <div className="text-[13px] font-semibold text-gray-800 mb-0.5 truncate">ERP 매뉴얼 업데이트</div>
                  <div className="text-[11px] text-gray-400">2026-02-08</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
