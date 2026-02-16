import { useWorkspaceStore } from '../../stores/workspaceStore';
import FunctionTabBar from './FunctionTabBar';
import FunctionContent from './FunctionContent';

interface CompanyWorkspaceViewProps {
  companyId: string;
}

export default function CompanyWorkspaceView({ companyId }: CompanyWorkspaceViewProps) {
  const workspace = useWorkspaceStore(state =>
    state.workspaces.find(w => w.company.companyId === companyId)
  );

  if (!workspace) {
    return (
      <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#9CA3AF' }}>
        워크스페이스를 찾을 수 없습니다.
      </div>
    );
  }

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0 }}>
      {/* 거래처 정보 헤더 */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '10px 20px',
          background: 'linear-gradient(135deg, #2E4A7A, #3B5998)',
          flexShrink: 0,
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <div
            style={{
              width: '32px',
              height: '32px',
              borderRadius: '8px',
              background: 'rgba(255,255,255,0.2)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#fff',
              fontWeight: 700,
              fontSize: '14px',
            }}
          >
            {workspace.company.companyName.charAt(0)}
          </div>
          <div>
            <div style={{ fontSize: '16px', fontWeight: 700, color: '#fff', lineHeight: 1.2 }}>
              {workspace.company.companyName}
            </div>
            <div style={{ fontSize: '11px', color: 'rgba(255,255,255,0.6)', marginTop: '2px' }}>
              {workspace.company.businessNumber || workspace.company.companyId}
            </div>
          </div>
          {workspace.company.companyType && (
            <span
              style={{
                padding: '3px 10px',
                borderRadius: '4px',
                background: 'rgba(255,255,255,0.15)',
                color: '#fff',
                fontSize: '11px',
                fontWeight: 500,
              }}
            >
              {workspace.company.companyType}
            </span>
          )}
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <span style={{ fontSize: '12px', color: 'rgba(255,255,255,0.5)' }}>
            오늘: <strong style={{ color: '#fff' }}>0</strong>건
          </span>
        </div>
      </div>

      {/* 2단계: 기능별 탭 바 */}
      <FunctionTabBar
        companyId={companyId}
        activeFunctionTab={workspace.activeFunctionTab}
      />

      {/* 3단계: 기능 콘텐츠 영역 */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0, overflow: 'hidden' }}>
        <FunctionContent workspace={workspace} />
      </div>
    </div>
  );
}
