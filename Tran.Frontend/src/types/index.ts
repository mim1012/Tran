export interface ApiResponse<T> {
  success: boolean;
  message?: string;
  data?: T;
  errors?: string[];
}

export interface LoginRequest {
  userId: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  userId: string;
  userName: string;
  companyId: string;
  companyName: string;
}

export interface User {
  userId: string;
  userName: string;
  companyId: string;
  companyName?: string;
}

export interface Company {
  companyId: string;
  companyName: string;
  companyType?: '고객사' | '공급사';
  businessNumber?: string;
  representative?: string;
  address?: string;
  phone?: string;
  email?: string;
  isActive: boolean;
  createdAt: string;
}

export interface Product {
  productId: number;
  productName: string;
  productCode?: string;
  category?: string;
  unit: string;
  defaultPrice: number;
  isActive: boolean;
  createdAt: string;
}

export interface Order {
  orderId: number;
  ownerCompanyId: string;
  companyId: string;
  orderDate: string;
  state: OrderState;
  totalAmount: number;
  memo?: string;
  createdAt: string;
  completedAt?: string;
  company?: Company;
  items: OrderItem[];
}

export interface OrderItem {
  orderItemId: number;
  orderId: number;
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineAmount: number;
  note?: string;
}

export const OrderState = {
  Draft: 0,
  Completed: 1,
  Cancelled: 2,
} as const;
export type OrderState = (typeof OrderState)[keyof typeof OrderState];

export interface Purchase {
  purchaseId: number;
  orderId?: number;
  ownerCompanyId: string;
  companyId: string;
  purchaseDate: string;
  state: PurchaseState;
  totalAmount: number;
  memo?: string;
  createdAt: string;
  deliveredAt?: string;
  company?: Company;
  items: PurchaseItem[];
}

export interface PurchaseItem {
  purchaseItemId: number;
  purchaseId: number;
  productId: number;
  productName: string;
  orderedQuantity: number;
  receivedQuantity: number;
  defectQuantity: number;
  unitPrice: number;
  lineAmount: number;
  note?: string;
}

export const PurchaseState = {
  PendingDelivery: 0,
  PartiallyDelivered: 1,
  Delivered: 2,
  Cancelled: 3,
} as const;
export type PurchaseState = (typeof PurchaseState)[keyof typeof PurchaseState];

export interface Sale {
  saleId: number;
  ownerCompanyId: string;
  companyId: string;
  saleDate: string;
  state: SaleState;
  totalAmount: number;
  memo?: string;
  createdAt: string;
  confirmedAt?: string;
  company?: Company;
  items: SaleItem[];
}

export interface SaleItem {
  saleItemId: number;
  saleId: number;
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineAmount: number;
  note?: string;
}

export const SaleState = {
  Draft: 0,
  Confirmed: 1,
  Cancelled: 2,
} as const;
export type SaleState = (typeof SaleState)[keyof typeof SaleState];

export interface Quotation {
  quotationId: number;
  ownerCompanyId: string;
  companyId: string;
  quotationDate: string;
  validUntil?: string;
  state: QuotationState;
  totalAmount: number;
  memo?: string;
  createdAt: string;
  sentAt?: string;
  confirmedAt?: string;
  company?: Company;
  items: QuotationItem[];
}

export interface QuotationItem {
  quotationItemId: number;
  quotationId: number;
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineAmount: number;
  note?: string;
}

export const QuotationState = {
  Draft: 0,
  Sent: 1,
  UnderReview: 2,
  Confirmed: 3,
  RevisionRequested: 4,
  Expired: 5,
} as const;
export type QuotationState = (typeof QuotationState)[keyof typeof QuotationState];

export interface Inventory {
  inventoryId: number;
  productId: number;
  confirmedQuantity: number;
  pendingInQuantity: number;
  pendingOutQuantity: number;
  safetyStock: number;
  lastUpdatedAt: string;
  product?: Product;
}

// ═══════════════════════════════════════
// 거래명세표 (Document)
// ═══════════════════════════════════════

export const DocumentState = {
  Draft: 0,
  Sent: 1,
  Received: 2,
  RevisionRequested: 3,
  Confirmed: 4,
  Superseded: 5,
  Cancelled: 6,
} as const;
export type DocumentState = (typeof DocumentState)[keyof typeof DocumentState];

export interface Document {
  documentId: string;
  parentDocumentId?: string;
  versionNumber: number;
  fromCompanyId: string;
  toCompanyId: string;
  state: DocumentState;
  stateVersion: number;
  totalAmount: number;
  contentHash?: string;
  createdBy: string;
  createdAt: string;
  sentAt?: string;
  confirmedAt?: string;
  transactionDate: string;
  memo?: string;
  internalMemo?: string;
  items: DocumentItem[];
}

export interface DocumentItem {
  itemId: string;
  documentId: string;
  itemName: string;
  quantity: number;
  unitPrice: number;
  optionText?: string;
  lineAmount: number;
  extraDataJson?: string;
}

export interface CreateDocumentRequest {
  toCompanyId: string;
  transactionDate: string;
  memo?: string;
  items: DocumentItemDto[];
}

export interface DocumentItemDto {
  itemName: string;
  quantity: number;
  unitPrice: number;
  optionText?: string;
}

export interface UpdateDocumentItemsRequest {
  items: DocumentItemDto[];
}

export interface DashboardSummary {
  totalOrders: number;
  approvedOrders: number;
  pendingOrders: number;
  lowStockItems: number;
  totalSalesAmount: number;
  totalPurchaseAmount: number;
  recentOrders: RecentOrderDto[];
}

export interface RecentOrderDto {
  orderId: number;
  companyName: string;
  orderDate: string;
  totalAmount: number;
  state: string;
}

// ═══════════════════════════════════════
// 워크스페이스 (3중 분할탭)
// ═══════════════════════════════════════

/** 기능별 탭 정의 */
export interface FunctionTab {
  id: string;
  label: string;
  icon: string;
}

/** 3단계 상세 뷰 타입 */
export type DetailViewType = 'list' | 'create' | 'detail';

/** 거래처별 워크스페이스 상태 */
export interface CompanyWorkspace {
  company: Company;
  activeFunctionTab: string;
  detailView: DetailViewType;
  detailId?: string | number;
}
