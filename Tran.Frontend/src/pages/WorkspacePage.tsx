import { useWorkspaceStore } from '../stores/workspaceStore';
import CompanyTabBar from '../components/workspace/CompanyTabBar';
import CompanySelection from '../components/workspace/CompanySelection';
import CompanyWorkspaceView from '../components/workspace/CompanyWorkspaceView';

export default function WorkspacePage() {
  const { workspaces, activeCompanyId, showCompanySelection } = useWorkspaceStore();

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0 }}>
      {/* 1단계: 거래처 탭 바 (항상 표시) */}
      <CompanyTabBar />

      {/* 콘텐츠 영역 */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0 }}>
        {showCompanySelection || workspaces.length === 0 ? (
          <CompanySelection />
        ) : activeCompanyId ? (
          <CompanyWorkspaceView companyId={activeCompanyId} />
        ) : (
          <div
            style={{
              flex: 1,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#9CA3AF',
              fontSize: '14px',
            }}
          >
            거래처를 선택해주세요
          </div>
        )}
      </div>

      {/* 하단 상태바 */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '6px 16px',
          background: '#F9FAFB',
          borderTop: '1px solid #E5E7EB',
          fontSize: '11px',
          color: '#9CA3AF',
          flexShrink: 0,
        }}
      >
        <span>
          {activeCompanyId && !showCompanySelection
            ? `작업 중: ${workspaces.find(w => w.company.companyId === activeCompanyId)?.company.companyName || ''}`
            : '준비'}
        </span>
        <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
          <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
            <span style={{ width: '6px', height: '6px', borderRadius: '50%', background: '#27AE60', display: 'inline-block' }} />
            연결 상태: 정상
          </span>
          <span>Tran v1.0</span>
        </div>
      </div>
    </div>
  );
}
