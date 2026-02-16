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
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        <Topbar title="대시보드" breadcrumb="대시보드" />
        <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
          <div style={{ color: '#9CA3AF', fontSize: '14px' }}>로딩 중...</div>
        </div>
      </div>
    );
  }

  const summaryCards = [
    {
      icon: 'fa-file-invoice',
      label: '이번 달 발주',
      value: summary?.totalOrders || 0,
      change: '12.5% 전월 대비',
      changeType: 'up' as const,
      iconBg: '#EBF0F7',
      iconColor: '#2E4A7A',
      accentColor: '#2E4A7A',
    },
    {
      icon: 'fa-check-circle',
      label: '승인 완료',
      value: summary?.approvedOrders || 0,
      change: '8.3% 전월 대비',
      changeType: 'up' as const,
      iconBg: '#E8F5E9',
      iconColor: '#27AE60',
      accentColor: '#27AE60',
    },
    {
      icon: 'fa-clock',
      label: '승인 대기',
      value: summary?.pendingOrders || 0,
      change: '5.2% 전월 대비',
      changeType: 'down' as const,
      iconBg: '#FFF3E0',
      iconColor: '#F39C12',
      accentColor: '#E74C3C',
    },
    {
      icon: 'fa-exclamation-triangle',
      label: '재고 부족 품목',
      value: summary?.lowStockItems || 0,
      change: '2건 증가',
      changeType: 'up' as const,
      iconBg: '#FFEBEE',
      iconColor: '#E74C3C',
      accentColor: '#E74C3C',
    },
  ];

  const quickActions = [
    { icon: 'fa-plus', label: '신규 발주 등록', iconBg: '#EBF0F7', iconColor: '#2E4A7A' },
    { icon: 'fa-box', label: '입고 처리', iconBg: '#E8F5E9', iconColor: '#27AE60' },
    { icon: 'fa-clipboard-list', label: '재고 실사', iconBg: '#FFF3E0', iconColor: '#F39C12' },
    { icon: 'fa-file-export', label: '리포트 생성', iconBg: '#F3E5F5', iconColor: '#8E44AD' },
  ];

  const notices = [
    { title: '2월 정기 재고 실사 안내', date: '2026-02-14', dotColor: '#2E4A7A' },
    { title: '시스템 점검 예정 (2/20)', date: '2026-02-13', dotColor: '#F39C12' },
    { title: '신규 거래처 등록 완료', date: '2026-02-10', dotColor: '#27AE60' },
    { title: 'ERP 매뉴얼 업데이트', date: '2026-02-08', dotColor: '#D1D5DB' },
  ];

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
      <Topbar title="대시보드" breadcrumb="대시보드" />

      <div className="flex-1 p-4 sm:p-6 overflow-y-auto">
        {/* Summary Cards */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-5 mb-5 sm:mb-7">
          {summaryCards.map((card, idx) => (
            <div
              key={idx}
              style={{
                background: '#fff',
                borderRadius: '12px',
                position: 'relative',
                overflow: 'hidden',
                boxShadow: '0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04)',
                transition: 'box-shadow 0.2s ease',
              }}
              className="p-4 sm:p-6"
            >
              {/* Background accent circle */}
              <div
                style={{
                  position: 'absolute',
                  top: '-16px',
                  right: '-16px',
                  width: '80px',
                  height: '80px',
                  borderRadius: '50%',
                  background: card.accentColor,
                  opacity: 0.06,
                }}
              />
              {/* Icon */}
              <div
                className="w-9 h-9 sm:w-11 sm:h-11 rounded-lg sm:rounded-xl mb-2.5 sm:mb-3.5"
                style={{
                  background: card.iconBg,
                  color: card.iconColor,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                }}
              >
                <i className={`fas ${card.icon} text-sm sm:text-lg`} />
              </div>
              {/* Label */}
              <div className="text-xs sm:text-[13px] text-gray-500 font-medium mb-1 sm:mb-1.5">
                {card.label}
              </div>
              {/* Value */}
              <div className="text-xl sm:text-[28px] font-extrabold text-gray-900" style={{ letterSpacing: '-0.02em', lineHeight: 1.1 }}>
                {card.value}
              </div>
              {/* Change */}
              <div
                className="text-[10px] sm:text-[11px] mt-1.5 sm:mt-2 flex items-center gap-1 font-medium"
                style={{
                  color: card.changeType === 'up' && card.accentColor === '#E74C3C' ? '#E74C3C' : card.changeType === 'up' ? '#27AE60' : '#E74C3C',
                }}
              >
                <i className={`fas fa-arrow-${card.changeType}`} style={{ fontSize: '9px' }} />
                {card.change}
              </div>
            </div>
          ))}
        </div>

        {/* Recent Orders Table */}
        <div className="mb-5 sm:mb-7">
          <div className="flex items-center justify-between mb-3 sm:mb-3.5">
            <h2 className="text-[15px] sm:text-base font-bold text-gray-900">최근 발주 내역</h2>
            <div
              style={{
                fontSize: '13px',
                color: '#2E4A7A',
                fontWeight: 600,
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                gap: '4px',
              }}
            >
              전체보기 <i className="fas fa-chevron-right" style={{ fontSize: '9px' }} />
            </div>
          </div>

          <div
            style={{
              background: '#fff',
              borderRadius: '12px',
              overflow: 'hidden',
              boxShadow: '0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04)',
            }}
          >
            <div className="overflow-x-auto">
              <table style={{ width: '100%', borderCollapse: 'collapse', minWidth: '500px' }}>
                <thead>
                  <tr>
                    {['발주번호', '거래처', '발주일', '금액', '상태'].map((header) => (
                      <th
                        key={header}
                        className="table-header"
                      >
                        {header}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {summary?.recentOrders && summary.recentOrders.length > 0 ? (
                    summary.recentOrders.map((order) => (
                      <tr key={order.orderId} style={{ transition: 'background 0.15s' }}>
                        <td className="table-cell font-semibold text-primary">
                          PO-{order.orderId.toString().padStart(6, '0')}
                        </td>
                        <td className="table-cell">
                          {order.companyName}
                        </td>
                        <td className="table-cell">
                          {new Date(order.orderDate).toLocaleDateString('ko-KR')}
                        </td>
                        <td className="table-cell font-semibold text-gray-900">
                          ₩{order.totalAmount.toLocaleString()}
                        </td>
                        <td className="table-cell">
                          <StatusBadge state={order.state} />
                        </td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td
                        colSpan={5}
                        className="px-5 py-8 text-center text-[13px] text-gray-400"
                      >
                        발주 내역이 없습니다.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>

        {/* Bottom Grid: Quick Actions + Notices */}
        <div className="grid grid-cols-1 lg:grid-cols-[2fr_1fr] gap-4 sm:gap-5">
          {/* Quick Actions */}
          <div
            style={{
              background: '#fff',
              borderRadius: '12px',
              boxShadow: '0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04)',
            }}
            className="p-4 sm:p-6"
          >
            <h3 className="text-[14px] sm:text-[15px] font-bold text-gray-900 mb-3 sm:mb-4">
              빠른 실행
            </h3>
            <div className="grid grid-cols-2 gap-2.5 sm:gap-3">
              {quickActions.map((action, idx) => (
                <div
                  key={idx}
                  className="quick-action-item"
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: '12px',
                    padding: '14px',
                    borderRadius: '10px',
                    border: '1px solid #F3F4F6',
                    cursor: 'pointer',
                    transition: 'all 0.15s ease',
                  }}
                >
                  <div
                    className="w-9 h-9 sm:w-10 sm:h-10 rounded-lg sm:rounded-[10px] flex-shrink-0"
                    style={{
                      background: action.iconBg,
                      color: action.iconColor,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                    }}
                  >
                    <i className={`fas ${action.icon} text-sm sm:text-[15px]`} />
                  </div>
                  <div className="text-xs sm:text-[13px] font-semibold text-gray-700">
                    {action.label}
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Notices */}
          <div
            style={{
              background: '#fff',
              borderRadius: '12px',
              boxShadow: '0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04)',
            }}
            className="p-4 sm:p-6"
          >
            <h3 className="text-[14px] sm:text-[15px] font-bold text-gray-900 mb-3 sm:mb-4">
              공지사항
            </h3>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
              {notices.map((notice, idx) => (
                <div
                  key={idx}
                  style={{
                    display: 'flex',
                    gap: '10px',
                    paddingBottom: idx < notices.length - 1 ? '14px' : '0',
                    borderBottom: idx < notices.length - 1 ? '1px solid #F9FAFB' : 'none',
                  }}
                >
                  <div
                    style={{
                      width: '7px',
                      height: '7px',
                      borderRadius: '50%',
                      background: notice.dotColor,
                      marginTop: '6px',
                      flexShrink: 0,
                    }}
                  />
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div
                      style={{
                        fontSize: '13px',
                        fontWeight: 600,
                        color: '#1F2937',
                        marginBottom: '3px',
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        whiteSpace: 'nowrap',
                      }}
                    >
                      {notice.title}
                    </div>
                    <div style={{ fontSize: '11px', color: '#9CA3AF' }}>
                      {notice.date}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function StatusBadge({ state }: { state: string }) {
  const stateMap: Record<string, { label: string; bg: string; color: string }> = {
    Completed: { label: '승인완료', bg: '#E8F5E9', color: '#27AE60' },
    Draft: { label: '작성중', bg: '#F5F5F5', color: '#666666' },
    Cancelled: { label: '취소', bg: '#FFEBEE', color: '#E74C3C' },
    Pending: { label: '대기중', bg: '#FFF3E0', color: '#F39C12' },
  };

  const info = stateMap[state] || { label: state, bg: '#F5F5F5', color: '#666' };

  return (
    <span
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: '4px',
        padding: '4px 10px',
        borderRadius: '9999px',
        fontSize: '12px',
        fontWeight: 600,
        background: info.bg,
        color: info.color,
      }}
    >
      {info.label}
    </span>
  );
}
