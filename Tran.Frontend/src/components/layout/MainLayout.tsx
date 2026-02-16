import { useState } from 'react';
import { Outlet } from 'react-router-dom';
import Sidebar from './Sidebar';

export default function MainLayout() {
  const [sidebarOpen, setSidebarOpen] = useState(false);

  return (
    <div style={{ display: 'flex', minHeight: '100vh' }}>
      <Sidebar isOpen={sidebarOpen} onClose={() => setSidebarOpen(false)} />
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        {/* Mobile header bar */}
        <div
          className="lg:hidden"
          style={{
            display: 'none',
            alignItems: 'center',
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
        <Outlet />
      </div>
    </div>
  );
}
