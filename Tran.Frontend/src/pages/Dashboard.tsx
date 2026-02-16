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
          <div className="text-gray-500">로딩 중...</div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex-1 flex flex-col">
      <Topbar title="대시보드" breadcrumb="대시보드" />
      
      <div className="flex-1 p-7 overflow-y-auto">
        {/* Summary Cards */}
        <div className="grid grid-cols-4 gap-5 mb-7">
          <div className="card p-6 relative overflow-hidden">
            <div className="absolute top-0 right-0 w-24 h-24 bg-primary opacity-5 rounded-full -mr-5 -mt-5"></div>
            <div className="w-12 h-12 rounded-2xl bg-blue-100 text-primary flex items-center justify-center text-xl mb-4">
              <i className="fas fa-file-invoice"></i>
            </div>
            <div className="text-sm text-gray-500 font-medium mb-1.5">이번 달 발주</div>
            <div className="text-3xl font-extrabold text-gray-900 tracking-tight">{summary?.totalOrders || 0}</div>
            <div className="text-xs text-success mt-2 flex items-center gap-1">
              <i className="fas fa-arrow-up"></i> 12.5% 전월 대비
            </div>
          </div>

          <div className="card p-6 relative overflow-hidden">
            <div className="absolute top-0 right-0 w-24 h-24 bg-success opacity-5 rounded-full -mr-5 -mt-5"></div>
            <div className="w-12 h-12 rounded-2xl bg-green-100 text-success flex items-center justify-center text-xl mb-4">
              <i className="fas fa-check-circle"></i>
            </div>
            <div className="text-sm text-gray-500 font-medium mb-1.5">승인 완료</div>
            <div className="text-3xl font-extrabold text-gray-900 tracking-tight">{summary?.approvedOrders || 0}</div>
            <div className="text-xs text-success mt-2 flex items-center gap-1">
              <i className="fas fa-arrow-up"></i> 8.3% 전월 대비
            </div>
          </div>

          <div className="card p-6 relative overflow-hidden">
            <div className="absolute top-0 right-0 w-24 h-24 bg-warning opacity-5 rounded-full -mr-5 -mt-5"></div>
            <div className="w-12 h-12 rounded-2xl bg-orange-100 text-warning flex items-center justify-center text-xl mb-4">
              <i className="fas fa-clock"></i>
            </div>
            <div className="text-sm text-gray-500 font-medium mb-1.5">승인 대기</div>
            <div className="text-3xl font-extrabold text-gray-900 tracking-tight">{summary?.pendingOrders || 0}</div>
            <div className="text-xs text-danger mt-2 flex items-center gap-1">
              <i className="fas fa-arrow-down"></i> 5.2% 전월 대비
            </div>
          </div>

          <div className="card p-6 relative overflow-hidden">
            <div className="absolute top-0 right-0 w-24 h-24 bg-danger opacity-5 rounded-full -mr-5 -mt-5"></div>
            <div className="w-12 h-12 rounded-2xl bg-red-100 text-danger flex items-center justify-center text-xl mb-4">
              <i className="fas fa-exclamation-triangle"></i>
            </div>
            <div className="text-sm text-gray-500 font-medium mb-1.5">재고 부족 품목</div>
            <div className="text-3xl font-extrabold text-gray-900 tracking-tight">{summary?.lowStockItems || 0}</div>
            <div className="text-xs text-danger mt-2 flex items-center gap-1">
              <i className="fas fa-arrow-up"></i> 2건 증가
            </div>
          </div>
        </div>

        {/* Recent Orders Table */}
        <div className="mb-5">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-bold text-gray-900">최근 발주 내역</h2>
            <div className="text-sm text-primary font-semibold cursor-pointer flex items-center gap-1">
              전체보기 <i className="fas fa-chevron-right text-[10px]"></i>
            </div>
          </div>
          
          <div className="card overflow-hidden">
            <table className="w-full">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">발주번호</th>
                  <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">거래처</th>
                  <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">발주일</th>
                  <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">금액</th>
                  <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">상태</th>
                </tr>
              </thead>
              <tbody>
                {summary?.recentOrders.map((order) => (
                  <tr key={order.orderId} className="hover:bg-gray-50 transition-colors">
                    <td className="px-5 py-3.5 text-sm font-semibold text-primary border-b border-gray-100">
                      PO-{order.orderId.toString().padStart(6, '0')}
                    </td>
                    <td className="px-5 py-3.5 text-sm text-gray-700 border-b border-gray-100">{order.companyName}</td>
                    <td className="px-5 py-3.5 text-sm text-gray-700 border-b border-gray-100">
                      {new Date(order.orderDate).toLocaleDateString('ko-KR')}
                    </td>
                    <td className="px-5 py-3.5 text-sm font-semibold text-gray-900 border-b border-gray-100">
                      ₩{order.totalAmount.toLocaleString()}
                    </td>
                    <td className="px-5 py-3.5 text-sm border-b border-gray-100">
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

        {/* Bottom Grid */}
        <div className="grid grid-cols-3 gap-5">
          <div className="card p-6 col-span-2">
            <h3 className="text-base font-bold text-gray-900 mb-4">빠른 실행</h3>
            <div className="grid grid-cols-2 gap-3">
              <div className="flex items-center gap-3 p-3.5 rounded-xl border border-gray-200 cursor-pointer hover:border-primary hover:bg-blue-50 transition-all">
                <div className="w-10 h-10 rounded-lg bg-blue-100 text-primary flex items-center justify-center text-base">
                  <i className="fas fa-plus"></i>
                </div>
                <div className="text-sm font-semibold text-gray-700">신규 발주 등록</div>
              </div>
              <div className="flex items-center gap-3 p-3.5 rounded-xl border border-gray-200 cursor-pointer hover:border-success hover:bg-green-50 transition-all">
                <div className="w-10 h-10 rounded-lg bg-green-100 text-success flex items-center justify-center text-base">
                  <i className="fas fa-box"></i>
                </div>
                <div className="text-sm font-semibold text-gray-700">입고 처리</div>
              </div>
              <div className="flex items-center gap-3 p-3.5 rounded-xl border border-gray-200 cursor-pointer hover:border-warning hover:bg-orange-50 transition-all">
                <div className="w-10 h-10 rounded-lg bg-orange-100 text-warning flex items-center justify-center text-base">
                  <i className="fas fa-clipboard-list"></i>
                </div>
                <div className="text-sm font-semibold text-gray-700">재고 실사</div>
              </div>
              <div className="flex items-center gap-3 p-3.5 rounded-xl border border-gray-200 cursor-pointer hover:border-purple hover:bg-purple-50 transition-all">
                <div className="w-10 h-10 rounded-lg bg-purple-100 text-purple flex items-center justify-center text-base">
                  <i className="fas fa-file-export"></i>
                </div>
                <div className="text-sm font-semibold text-gray-700">리포트 생성</div>
              </div>
            </div>
          </div>

          <div className="card p-6">
            <h3 className="text-base font-bold text-gray-900 mb-4">공지사항</h3>
            <div className="space-y-3">
              <div className="flex gap-3 pb-3 border-b border-gray-100">
                <div className="w-2 h-2 rounded-full bg-primary mt-1.5 flex-shrink-0"></div>
                <div className="flex-1">
                  <div className="text-sm font-semibold text-gray-800 mb-1">2월 정기 재고 실사 안내</div>
                  <div className="text-xs text-gray-400">2026-02-14</div>
                </div>
              </div>
              <div className="flex gap-3 pb-3 border-b border-gray-100">
                <div className="w-2 h-2 rounded-full bg-warning mt-1.5 flex-shrink-0"></div>
                <div className="flex-1">
                  <div className="text-sm font-semibold text-gray-800 mb-1">시스템 점검 예정 (2/20)</div>
                  <div className="text-xs text-gray-400">2026-02-13</div>
                </div>
              </div>
              <div className="flex gap-3 pb-3 border-b border-gray-100">
                <div className="w-2 h-2 rounded-full bg-success mt-1.5 flex-shrink-0"></div>
                <div className="flex-1">
                  <div className="text-sm font-semibold text-gray-800 mb-1">신규 거래처 등록 완료</div>
                  <div className="text-xs text-gray-400">2026-02-10</div>
                </div>
              </div>
              <div className="flex gap-3">
                <div className="w-2 h-2 rounded-full bg-gray-300 mt-1.5 flex-shrink-0"></div>
                <div className="flex-1">
                  <div className="text-sm font-semibold text-gray-800 mb-1">ERP 매뉴얼 업데이트</div>
                  <div className="text-xs text-gray-400">2026-02-08</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
