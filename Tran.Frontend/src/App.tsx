import { BrowserRouter, Routes, Route } from 'react-router-dom';
import MainLayout from './components/layout/MainLayout';
import Dashboard from './pages/Dashboard';
import OrderManagement from './pages/OrderManagement';
import ProductManagement from './pages/ProductManagement';
import QuotationManagement from './pages/QuotationManagement';
import PurchaseManagement from './pages/PurchaseManagement';
import SaleManagement from './pages/SaleManagement';
import InventoryManagement from './pages/InventoryManagement';
import CompanyManagement from './pages/CompanyManagement';
import Reports from './pages/Reports';
import Settings from './pages/Settings';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<MainLayout />}>
          <Route path="/" element={<Dashboard />} />
          <Route path="/orders" element={<OrderManagement />} />
          <Route path="/products" element={<ProductManagement />} />
          <Route path="/quotations" element={<QuotationManagement />} />
          <Route path="/purchases" element={<PurchaseManagement />} />
          <Route path="/sales" element={<SaleManagement />} />
          <Route path="/inventory" element={<InventoryManagement />} />
          <Route path="/companies" element={<CompanyManagement />} />
          <Route path="/reports" element={<Reports />} />
          <Route path="/settings" element={<Settings />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
