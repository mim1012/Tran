import Topbar from '../components/layout/Topbar';
import { useWorkspaceStore } from '../stores/workspaceStore';
import CompanyTabBar from '../components/workspace/CompanyTabBar';
import CompanySelection from '../components/workspace/CompanySelection';
import CompanyWorkspaceView from '../components/workspace/CompanyWorkspaceView';

export default function WorkspacePage() {
  const { workspaces, activeCompanyId, showCompanySelection } = useWorkspaceStore();

  return (
    <div className="flex-1 flex flex-col">
      <Topbar title="작업 공간" breadcrumb="작업 공간" />
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* 거래처 탭 바 (항상 표시) */}
        <CompanyTabBar />

        {/* 콘텐츠 영역 */}
        <div className="flex-1 flex flex-col min-h-0">
          {showCompanySelection || workspaces.length === 0 ? (
            <CompanySelection />
          ) : activeCompanyId ? (
            <CompanyWorkspaceView companyId={activeCompanyId} />
          ) : (
            <div className="flex-1 flex items-center justify-center text-gray-400 text-sm">
              거래처를 선택해주세요
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
