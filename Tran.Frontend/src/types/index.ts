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

export enum OrderState {
  Draft = 0,
  Completed = 1,
  Cancelled = 2,
}

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

export enum PurchaseState {
  PendingDelivery = 0,
  PartiallyDelivered = 1,
  Delivered = 2,
  Cancelled = 3,
}

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

export enum SaleState {
  Draft = 0,
  Confirmed = 1,
  Cancelled = 2,
}

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

export enum QuotationState {
  Draft = 0,
  Sent = 1,
  UnderReview = 2,
  Confirmed = 3,
  RevisionRequested = 4,
  Expired = 5,
}

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

export enum DocumentState {
  Draft = 0,
  Sent = 1,
  Received = 2,
  RevisionRequested = 3,
  Confirmed = 4,
  Superseded = 5,
  Cancelled = 6,
}

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
