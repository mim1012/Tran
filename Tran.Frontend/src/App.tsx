import { BrowserRouter, Routes, Route } from 'react-router-dom';
import MainLayout from './components/layout/MainLayout';
import Dashboard from './pages/Dashboard';
import ProductManagement from './pages/ProductManagement';
import CompanyManagement from './pages/CompanyManagement';
import DocumentManagement from './pages/DocumentManagement';
import Login from './pages/Login';
import Reports from './pages/Reports';
import Settings from './pages/Settings';
import WorkspacePage from './pages/WorkspacePage';
import Toast from './components/common/Toast';

function App() {
  return (
    <BrowserRouter>
      <Toast />
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route element={<MainLayout />}>
          <Route path="/" element={<Dashboard />} />
          <Route path="/workspace" element={<WorkspacePage />} />
          <Route path="/documents" element={<DocumentManagement />} />
          <Route path="/products" element={<ProductManagement />} />
          <Route path="/companies" element={<CompanyManagement />} />
          <Route path="/reports" element={<Reports />} />
          <Route path="/settings" element={<Settings />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
