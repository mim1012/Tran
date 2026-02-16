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
      <div className="flex-1 flex items-center justify-center text-gray-400 text-sm">
        워크스페이스를 찾을 수 없습니다.
      </div>
    );
  }

  const typeBadge = workspace.company.companyType === '공급사'
    ? { cls: 'pending', label: '공급사' }
    : { cls: 'approved', label: workspace.company.companyType || '고객사' };

  return (
    <div className="flex-1 flex flex-col min-h-0">
      {/* 거래처 정보 헤더 - card 스타일 적용 */}
      <div
        className="flex-shrink-0"
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '12px 24px',
          background: '#FEFEFE',
          borderBottom: '1px solid #E5E7EB',
        }}
      >
        <div className="flex items-center gap-3">
          <div
            style={{
              width: '36px',
              height: '36px',
              borderRadius: '10px',
              background: 'linear-gradient(135deg, #2E4A7A, #4A6FA5)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#fff',
              fontWeight: 700,
              fontSize: '14px',
              flexShrink: 0,
            }}
          >
            {workspace.company.companyName.charAt(0)}
          </div>
          <div>
            <div className="text-[15px] font-bold text-gray-900" style={{ lineHeight: 1.2 }}>
              {workspace.company.companyName}
            </div>
            <div className="text-[11px] text-gray-400 mt-0.5">
              {workspace.company.businessNumber || workspace.company.companyId}
            </div>
          </div>
          <span className={`status-badge ${typeBadge.cls}`} style={{ marginLeft: '4px' }}>
            <span className="w-1.5 h-1.5 rounded-full bg-current" />
            {typeBadge.label}
          </span>
        </div>

        <div className="flex items-center gap-3">
          <span className="text-xs text-gray-400">
            오늘: <strong className="text-gray-700">0</strong>건
          </span>
        </div>
      </div>

      {/* 기능별 탭 바 */}
      <FunctionTabBar
        companyId={companyId}
        activeFunctionTab={workspace.activeFunctionTab}
      />

      {/* 기능 콘텐츠 영역 */}
      <div className="flex-1 flex flex-col min-h-0 overflow-hidden">
        <FunctionContent workspace={workspace} />
      </div>
    </div>
  );
}
