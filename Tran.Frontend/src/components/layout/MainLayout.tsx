import { useState } from 'react';
import { Outlet } from 'react-router-dom';
import Sidebar from './Sidebar';

export default function MainLayout() {
  const [sidebarOpen, setSidebarOpen] = useState(false);

  return (
    <div className="flex min-h-screen">
      <Sidebar isOpen={sidebarOpen} onClose={() => setSidebarOpen(false)} />
      <div className="flex-1 flex flex-col min-w-0" style={{ background: '#F5F6F8' }}>
        {/* Mobile header bar */}
        <div
          className="flex lg:hidden items-center flex-shrink-0"
          style={{
            height: '56px',
            padding: '0 16px',
            background: '#fff',
            borderBottom: '1px solid #E5E7EB',
          }}
        >
          <button
            onClick={() => setSidebarOpen(true)}
            style={{
              width: '36px',
              height: '36px',
              borderRadius: '8px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: '#4B5563',
              background: 'transparent',
              border: 'none',
              cursor: 'pointer',
            }}
          >
            <i className="fas fa-bars" style={{ fontSize: '18px' }} />
          </button>
          <div style={{ marginLeft: '12px', fontSize: '16px', fontWeight: 700, color: '#111827' }}>
            Tran ERP
          </div>
        </div>
        {/* Content area: gray gutter + white card container */}
        <div className="flex-1 flex flex-col min-h-0 p-3 lg:p-8">
          <div
            className="flex-1 flex flex-col min-h-0 bg-white rounded-2xl overflow-hidden"
            style={{ boxShadow: '0 1px 4px rgba(0,0,0,0.08)' }}
          >
            <Outlet />
          </div>
        </div>
      </div>
    </div>
  );
}
