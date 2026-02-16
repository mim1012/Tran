import { FUNCTION_TABS, useWorkspaceStore } from '../../stores/workspaceStore';

interface FunctionTabBarProps {
  companyId: string;
  activeFunctionTab: string;
}

export default function FunctionTabBar({ companyId, activeFunctionTab }: FunctionTabBarProps) {
  const { setFunctionTab } = useWorkspaceStore();

  return (
    <div
      className="flex items-center overflow-x-auto flex-shrink-0 px-3 sm:px-6 scrollbar-hide"
      style={{
        background: '#fff',
        borderBottom: '2px solid #E5E7EB',
        gap: '0',
        WebkitOverflowScrolling: 'touch',
      }}
    >
      {FUNCTION_TABS.map(tab => {
        const isActive = tab.id === activeFunctionTab;
        return (
          <button
            key={tab.id}
            onClick={() => setFunctionTab(companyId, tab.id)}
            className={isActive ? 'function-tab-active' : ''}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '6px',
              padding: '10px 12px',
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
              flexShrink: 0,
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
