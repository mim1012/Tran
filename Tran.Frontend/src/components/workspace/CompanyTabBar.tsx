import { useWorkspaceStore } from '../../stores/workspaceStore';

export default function CompanyTabBar() {
  const {
    workspaces,
    activeCompanyId,
    setActiveCompany,
    closeCompany,
    setShowCompanySelection,
    showCompanySelection,
  } = useWorkspaceStore();

  return (
    <div
      className="flex items-center overflow-x-auto flex-shrink-0 scrollbar-hide px-3 sm:px-4"
      style={{
        background: '#F9FAFB',
        borderBottom: '1px solid #E5E7EB',
        minHeight: '42px',
        gap: '4px',
        WebkitOverflowScrolling: 'touch',
      }}
    >
      {/* 거래처 선택 버튼 */}
      <button
        onClick={() => setShowCompanySelection(true)}
        className="company-tab-btn"
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '6px',
          padding: '6px 12px',
          borderRadius: '6px',
          border: showCompanySelection ? '1px solid #2E4A7A' : '1px solid transparent',
          background: showCompanySelection ? '#EBF0F7' : 'transparent',
          color: showCompanySelection ? '#2E4A7A' : '#6B7280',
          fontSize: '12.5px',
          fontWeight: showCompanySelection ? 600 : 500,
          cursor: 'pointer',
          transition: 'all 0.15s ease',
          whiteSpace: 'nowrap',
          flexShrink: 0,
        }}
      >
        <i className="fas fa-th-large" style={{ fontSize: '11px' }} />
        거래처 선택
      </button>

      {/* 구분선 */}
      {workspaces.length > 0 && (
        <div
          style={{
            width: '1px',
            height: '20px',
            background: '#D1D5DB',
            margin: '0 6px',
            flexShrink: 0,
          }}
        />
      )}

      {/* 거래처 탭 목록 */}
      {workspaces.map(ws => {
        const isActive = ws.company.companyId === activeCompanyId && !showCompanySelection;
        return (
          <div
            key={ws.company.companyId}
            className="company-tab-item"
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '6px',
              padding: '6px 12px',
              borderRadius: '6px',
              border: isActive ? '1px solid #2E4A7A' : '1px solid transparent',
              background: isActive ? '#EBF0F7' : 'transparent',
              cursor: 'pointer',
              transition: 'all 0.15s ease',
              whiteSpace: 'nowrap',
              flexShrink: 0,
            }}
            onClick={() => setActiveCompany(ws.company.companyId)}
          >
            {/* 거래처 아이콘 */}
            <div
              style={{
                width: '22px',
                height: '22px',
                borderRadius: '6px',
                background: isActive
                  ? 'linear-gradient(135deg, #2E4A7A, #4A6FA5)'
                  : '#D1D5DB',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontSize: '10px',
                fontWeight: 700,
                color: '#fff',
                flexShrink: 0,
              }}
            >
              {ws.company.companyName.charAt(0)}
            </div>

            <span
              style={{
                fontSize: '12.5px',
                fontWeight: isActive ? 600 : 400,
                color: isActive ? '#2E4A7A' : '#6B7280',
                maxWidth: '100px',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
              }}
            >
              {ws.company.companyName}
            </span>

            {/* 닫기 버튼 */}
            <button
              onClick={e => {
                e.stopPropagation();
                closeCompany(ws.company.companyId);
              }}
              style={{
                width: '18px',
                height: '18px',
                borderRadius: '4px',
                border: 'none',
                background: 'transparent',
                color: '#9CA3AF',
                fontSize: '10px',
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                transition: 'all 0.1s ease',
                padding: 0,
                marginLeft: '2px',
              }}
              title={`${ws.company.companyName} 닫기`}
            >
              <i className="fas fa-times" style={{ fontSize: '9px' }} />
            </button>
          </div>
        );
      })}

      {/* 거래처 추가 버튼 */}
      <button
        onClick={() => setShowCompanySelection(true)}
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '4px',
          padding: '6px 10px',
          borderRadius: '6px',
          border: '1px dashed #D1D5DB',
          background: 'transparent',
          color: '#9CA3AF',
          fontSize: '12px',
          cursor: 'pointer',
          transition: 'all 0.15s ease',
          whiteSpace: 'nowrap',
          flexShrink: 0,
        }}
      >
        <i className="fas fa-plus" style={{ fontSize: '10px' }} />
        추가
      </button>
    </div>
  );
}
