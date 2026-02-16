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
      style={{
        display: 'flex',
        alignItems: 'center',
        background: '#1E3A5F',
        minHeight: '40px',
        padding: '0 4px',
        gap: '2px',
        overflowX: 'auto',
        flexShrink: 0,
      }}
    >
      {/* 거래처 선택 버튼 */}
      <button
        onClick={() => setShowCompanySelection(true)}
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '6px',
          padding: '6px 14px',
          margin: '3px 2px',
          borderRadius: '6px 6px 0 0',
          border: 'none',
          background: showCompanySelection ? '#3B5998' : 'transparent',
          color: showCompanySelection ? '#fff' : 'rgba(255,255,255,0.6)',
          fontSize: '12.5px',
          fontWeight: 500,
          cursor: 'pointer',
          transition: 'all 0.15s ease',
          whiteSpace: 'nowrap',
          flexShrink: 0,
        }}
        onMouseEnter={e => {
          if (!showCompanySelection) e.currentTarget.style.background = 'rgba(255,255,255,0.08)';
        }}
        onMouseLeave={e => {
          if (!showCompanySelection) e.currentTarget.style.background = 'transparent';
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
            background: 'rgba(255,255,255,0.15)',
            margin: '0 4px',
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
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '4px',
              padding: '6px 10px',
              margin: '3px 1px',
              borderRadius: '6px 6px 0 0',
              background: isActive ? '#3B5998' : 'transparent',
              cursor: 'pointer',
              transition: 'all 0.15s ease',
              whiteSpace: 'nowrap',
              flexShrink: 0,
            }}
            onClick={() => {
              setActiveCompany(ws.company.companyId);
            }}
            onMouseEnter={e => {
              if (!isActive) e.currentTarget.style.background = 'rgba(255,255,255,0.08)';
            }}
            onMouseLeave={e => {
              if (!isActive) e.currentTarget.style.background = 'transparent';
            }}
          >
            {/* 거래처 아이콘 */}
            <div
              style={{
                width: '20px',
                height: '20px',
                borderRadius: '4px',
                background: isActive ? 'rgba(255,255,255,0.2)' : 'rgba(255,255,255,0.1)',
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
                color: isActive ? '#fff' : 'rgba(255,255,255,0.7)',
                maxWidth: '120px',
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
                color: 'rgba(255,255,255,0.4)',
                fontSize: '12px',
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                transition: 'all 0.1s ease',
                padding: 0,
                marginLeft: '2px',
              }}
              onMouseEnter={e => {
                e.currentTarget.style.background = 'rgba(255,255,255,0.15)';
                e.currentTarget.style.color = '#fff';
              }}
              onMouseLeave={e => {
                e.currentTarget.style.background = 'transparent';
                e.currentTarget.style.color = 'rgba(255,255,255,0.4)';
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
          padding: '6px 12px',
          margin: '3px 2px',
          borderRadius: '6px',
          border: 'none',
          background: 'transparent',
          color: 'rgba(255,255,255,0.5)',
          fontSize: '12px',
          cursor: 'pointer',
          transition: 'all 0.15s ease',
          whiteSpace: 'nowrap',
          flexShrink: 0,
        }}
        onMouseEnter={e => {
          e.currentTarget.style.background = 'rgba(255,255,255,0.08)';
          e.currentTarget.style.color = 'rgba(255,255,255,0.8)';
        }}
        onMouseLeave={e => {
          e.currentTarget.style.background = 'transparent';
          e.currentTarget.style.color = 'rgba(255,255,255,0.5)';
        }}
      >
        <i className="fas fa-plus" style={{ fontSize: '10px' }} />
        거래처
      </button>
    </div>
  );
}
