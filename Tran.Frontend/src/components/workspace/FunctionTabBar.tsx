import { FUNCTION_TABS, useWorkspaceStore } from '../../stores/workspaceStore';

interface FunctionTabBarProps {
  companyId: string;
  activeFunctionTab: string;
}

export default function FunctionTabBar({ companyId, activeFunctionTab }: FunctionTabBarProps) {
  const { setFunctionTab } = useWorkspaceStore();

  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        background: '#fff',
        borderBottom: '2px solid #E5E7EB',
        padding: '0 16px',
        gap: '0',
        overflowX: 'auto',
        flexShrink: 0,
      }}
    >
      {FUNCTION_TABS.map(tab => {
        const isActive = tab.id === activeFunctionTab;
        return (
          <button
            key={tab.id}
            onClick={() => setFunctionTab(companyId, tab.id)}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '7px',
              padding: '12px 18px',
              border: 'none',
              background: 'transparent',
              color: isActive ? '#2E4A7A' : '#6B7280',
              fontSize: '13px',
              fontWeight: isActive ? 700 : 500,
              cursor: 'pointer',
              transition: 'all 0.15s ease',
              borderBottom: isActive ? '2px solid #2E4A7A' : '2px solid transparent',
              marginBottom: '-2px',
              whiteSpace: 'nowrap',
              position: 'relative',
            }}
            onMouseEnter={e => {
              if (!isActive) {
                e.currentTarget.style.color = '#374151';
                e.currentTarget.style.background = '#F9FAFB';
              }
            }}
            onMouseLeave={e => {
              if (!isActive) {
                e.currentTarget.style.color = '#6B7280';
                e.currentTarget.style.background = 'transparent';
              }
            }}
          >
            <i className={`fas ${tab.icon}`} style={{ fontSize: '12px' }} />
            {tab.label}
          </button>
        );
      })}
    </div>
  );
}
