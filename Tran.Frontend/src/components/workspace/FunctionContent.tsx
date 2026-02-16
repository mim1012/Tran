import type { CompanyWorkspace } from '../../types';
import OrderManagement from '../../pages/OrderManagement';
import PurchaseManagement from '../../pages/PurchaseManagement';
import SaleManagement from '../../pages/SaleManagement';
import InventoryManagement from '../../pages/InventoryManagement';
import ProductManagement from '../../pages/ProductManagement';
import QuotationManagement from '../../pages/QuotationManagement';
import DocumentManagement from '../../pages/DocumentManagement';

interface FunctionContentProps {
  workspace: CompanyWorkspace;
}

export default function FunctionContent({ workspace }: FunctionContentProps) {
  const { activeFunctionTab } = workspace;

  switch (activeFunctionTab) {
    case 'orders':
      return <OrderManagement />;
    case 'purchases':
      return <PurchaseManagement />;
    case 'sales':
      return <SaleManagement />;
    case 'inventory':
      return <InventoryManagement />;
    case 'products':
      return <ProductManagement />;
    case 'quotations':
      return <QuotationManagement />;
    case 'documents':
      return <DocumentManagement />;
    default:
      return <OrderManagement />;
  }
}
