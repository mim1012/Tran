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
  const companyId = workspace.company.companyId;

  switch (activeFunctionTab) {
    case 'orders':
      return <OrderManagement embedded companyId={companyId} />;
    case 'purchases':
      return <PurchaseManagement embedded companyId={companyId} />;
    case 'sales':
      return <SaleManagement embedded companyId={companyId} />;
    case 'inventory':
      return <InventoryManagement embedded companyId={companyId} />;
    case 'products':
      return <ProductManagement embedded companyId={companyId} />;
    case 'quotations':
      return <QuotationManagement embedded companyId={companyId} />;
    case 'documents':
      return <DocumentManagement embedded companyId={companyId} />;
    default:
      return <OrderManagement embedded companyId={companyId} />;
  }
}
