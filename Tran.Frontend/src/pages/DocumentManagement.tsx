import { useEffect, useState } from 'react';
import Topbar from '../components/layout/Topbar';
import apiClient from '../services/api';
import type { Document, Company, Product, CreateDocumentRequest, DocumentItemDto } from '../types';
import { DocumentState } from '../types';
import { useToastStore } from '../stores/toastStore';
import ConfirmDialog from '../components/common/ConfirmDialog';

const stateLabels: Record<number, { label: string; cls: string }> = {
  [DocumentState.Draft]: { label: '작성중', cls: 'draft' },
  [DocumentState.Sent]: { label: '전송됨', cls: 'processing' },
  [DocumentState.Received]: { label: '수신됨', cls: 'pending' },
  [DocumentState.RevisionRequested]: { label: '수정요청', cls: 'pending' },
  [DocumentState.Confirmed]: { label: '확정', cls: 'approved' },
  [DocumentState.Superseded]: { label: '대체됨', cls: 'draft' },
  [DocumentState.Cancelled]: { label: '취소', cls: 'rejected' },
};

const PAGE_SIZE = 10;

interface DocumentManagementProps {
  embedded?: boolean;
  companyId?: string;
}

export default function DocumentManagement({ embedded, companyId }: DocumentManagementProps) {
  const [documents, setDocuments] = useState<Document[]>([]);
  const [companies, setCompanies] = useState<Company[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [filterState, setFilterState] = useState('all');
  const [searchKeyword, setSearchKeyword] = useState('');
  const [currentPage, setCurrentPage] = useState(1);

  // Modal state
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showDetailModal, setShowDetailModal] = useState(false);
  const [selectedDocument, setSelectedDocument] = useState<Document | null>(null);
  const [isEditingItems, setIsEditingItems] = useState(false);

  // Create form state
  const [createForm, setCreateForm] = useState<{
    toCompanyId: string;
    transactionDate: string;
    memo: string;
    items: DocumentItemDto[];
  }>({
    toCompanyId: '',
    transactionDate: new Date().toISOString().split('T')[0],
    memo: '',
    items: [{ itemName: '', quantity: 1, unitPrice: 0 }],
  });

  // Edit items state
  const [editItems, setEditItems] = useState<DocumentItemDto[]>([]);

  // Confirm dialog state
  const [confirmDialog, setConfirmDialog] = useState<{
    open: boolean;
    docId: string;
  }>({ open: false, docId: '' });

  const addToast = useToastStore((s) => s.addToast);

  useEffect(() => {
    fetchDocuments();
    fetchCompanies();
    fetchProducts();
  }, [companyId]);

  const fetchDocuments = async () => {
    try {
      const endpoint = companyId ? `/documents/company/${companyId}` : '/documents';
      const res = await apiClient.get(endpoint);
      setDocuments(res.data.data || []);
    } catch {
      addToast({ type: 'error', message: '거래명세표를 불러올 수 없습니다.' });
    } finally { setLoading(false); }
  };

  const fetchCompanies = async () => {
    try {
      const res = await apiClient.get('/companies');
      setCompanies(res.data.data || []);
    } catch {
      addToast({ type: 'error', message: '거래처 목록을 불러올 수 없습니다.' });
    }
  };

  const fetchProducts = async () => {
    try {
      const res = await apiClient.get('/products');
      setProducts((res.data.data || []).filter((p: Product) => p.isActive));
    } catch {
      addToast({ type: 'error', message: '품목 목록을 불러올 수 없습니다.' });
    }
  };

  const getCompanyName = (companyId: string) => {
    return companies.find(c => c.companyId === companyId)?.companyName || companyId;
  };

  const filtered = documents.filter(d => {
    if (filterState !== 'all' && String(d.state) !== filterState) return false;
    if (searchKeyword) {
      const from = getCompanyName(d.fromCompanyId);
      const to = getCompanyName(d.toCompanyId);
      if (!from.includes(searchKeyword) && !to.includes(searchKeyword)) return false;
    }
    return true;
  });

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const paginated = filtered.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const badge = (state: number) => {
    const s = stateLabels[state] || { label: String(state), cls: 'draft' };
    return <span className={`status-badge ${s.cls}`}><span className="w-1.5 h-1.5 rounded-full bg-current" />{s.label}</span>;
  };

  // ── Create Document ──
  const handleCreate = async () => {
    try {
      const validItems = createForm.items.filter(i => i.itemName.trim());
      if (!createForm.toCompanyId || validItems.length === 0) {
        addToast({ type: 'warning', message: '수신 거래처와 최소 1개 품목을 입력해주세요.' });
        return;
      }
      const request: CreateDocumentRequest = {
        toCompanyId: createForm.toCompanyId,
        transactionDate: createForm.transactionDate,
        memo: createForm.memo || undefined,
        items: validItems,
      };
      await apiClient.post('/documents', request);
      setShowCreateModal(false);
      resetCreateForm();
      fetchDocuments();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message || '생성에 실패했습니다.';
      addToast({ type: 'error', message: msg });
    }
  };

  const resetCreateForm = () => {
    setCreateForm({
      toCompanyId: '',
      transactionDate: new Date().toISOString().split('T')[0],
      memo: '',
      items: [{ itemName: '', quantity: 1, unitPrice: 0 }],
    });
  };

  const addCreateItem = () => {
    setCreateForm(f => ({ ...f, items: [...f.items, { itemName: '', quantity: 1, unitPrice: 0 }] }));
  };

  const removeCreateItem = (idx: number) => {
    setCreateForm(f => ({ ...f, items: f.items.filter((_, i) => i !== idx) }));
  };

  const updateCreateItem = (idx: number, field: keyof DocumentItemDto, value: string | number) => {
    setCreateForm(f => ({
      ...f,
      items: f.items.map((item, i) => i === idx ? { ...item, [field]: value } : item),
    }));
  };

  const createItemsTotal = createForm.items.reduce((sum, i) => sum + i.quantity * i.unitPrice, 0);

  // ── Detail / Edit Items ──
  const openDetail = async (doc: Document) => {
    try {
      const res = await apiClient.get(`/documents/${doc.documentId}`);
      setSelectedDocument(res.data.data);
      setIsEditingItems(false);
      setShowDetailModal(true);
    } catch {
      addToast({ type: 'error', message: '문서 조회에 실패했습니다.' });
    }
  };

  const startEditItems = () => {
    if (!selectedDocument) return;
    setEditItems(selectedDocument.items.map(i => ({
      itemName: i.itemName,
      quantity: i.quantity,
      unitPrice: i.unitPrice,
      optionText: i.optionText,
    })));
    setIsEditingItems(true);
  };

  const saveEditItems = async () => {
    if (!selectedDocument) return;
    try {
      const validItems = editItems.filter(i => i.itemName.trim());
      await apiClient.put(`/documents/${selectedDocument.documentId}/items`, { items: validItems });
      const res = await apiClient.get(`/documents/${selectedDocument.documentId}`);
      setSelectedDocument(res.data.data);
      setIsEditingItems(false);
      fetchDocuments();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message || '품목 수정에 실패했습니다.';
      addToast({ type: 'error', message: msg });
    }
  };

  const addEditItem = () => {
    setEditItems(items => [...items, { itemName: '', quantity: 1, unitPrice: 0 }]);
  };

  const removeEditItem = (idx: number) => {
    setEditItems(items => items.filter((_, i) => i !== idx));
  };

  const updateEditItem = (idx: number, field: keyof DocumentItemDto, value: string | number) => {
    setEditItems(items => items.map((item, i) => i === idx ? { ...item, [field]: value } : item));
  };

  // ── Send Document ──
  const handleSendClick = (docId: string) => {
    setConfirmDialog({ open: true, docId });
  };

  const handleSendConfirm = async () => {
    const docId = confirmDialog.docId;
    setConfirmDialog({ open: false, docId: '' });
    try {
      await apiClient.post(`/documents/${docId}/send`);
      addToast({ type: 'success', message: '거래명세표가 전송되었습니다.' });
      fetchDocuments();
      if (selectedDocument?.documentId === docId) {
        const res = await apiClient.get(`/documents/${docId}`);
        setSelectedDocument(res.data.data);
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message || '전송에 실패했습니다.';
      addToast({ type: 'error', message: msg });
    }
  };

  // ── Product select helper ──
  const handleProductSelect = (
    idx: number,
    productName: string,
    updateFn: (idx: number, field: keyof DocumentItemDto, value: string | number) => void
  ) => {
    const product = products.find(p => p.productName === productName);
    updateFn(idx, 'itemName', productName);
    if (product) {
      updateFn(idx, 'unitPrice', product.defaultPrice);
    }
  };

  // ── Render Item Table (reusable for create & edit) ──
  const renderItemTable = (
    items: DocumentItemDto[],
    updateItem: (idx: number, field: keyof DocumentItemDto, value: string | number) => void,
    removeItem: (idx: number) => void,
    addItem: () => void,
  ) => (
    <div>
      <div className="overflow-x-auto">
        <table className="w-full text-[13px]">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-3 py-2 text-left text-[11px] font-semibold text-gray-500 border-b">품목명</th>
              <th className="px-3 py-2 text-right text-[11px] font-semibold text-gray-500 border-b w-20">수량</th>
              <th className="px-3 py-2 text-right text-[11px] font-semibold text-gray-500 border-b w-28">단가</th>
              <th className="px-3 py-2 text-right text-[11px] font-semibold text-gray-500 border-b w-28">금액</th>
              <th className="px-3 py-2 text-left text-[11px] font-semibold text-gray-500 border-b w-24">비고</th>
              <th className="px-3 py-2 border-b w-10"></th>
            </tr>
          </thead>
          <tbody>
            {items.map((item, idx) => (
              <tr key={idx} className="border-b border-gray-100">
                <td className="px-3 py-1.5">
                  <select
                    value={item.itemName}
                    onChange={e => handleProductSelect(idx, e.target.value, updateItem)}
                    className="w-full px-2 py-1.5 rounded border border-gray-200 text-[13px] bg-white focus:border-primary outline-none"
                  >
                    <option value="">-- 선택 --</option>
                    {products.map(p => (
                      <option key={p.productId} value={p.productName}>{p.productName}</option>
                    ))}
                  </select>
                </td>
                <td className="px-3 py-1.5">
                  <input
                    type="number"
                    min="0"
                    value={item.quantity}
                    onChange={e => updateItem(idx, 'quantity', Number(e.target.value))}
                    className="w-full px-2 py-1.5 rounded border border-gray-200 text-[13px] text-right bg-white focus:border-primary outline-none"
                  />
                </td>
                <td className="px-3 py-1.5">
                  <input
                    type="number"
                    min="0"
                    value={item.unitPrice}
                    onChange={e => updateItem(idx, 'unitPrice', Number(e.target.value))}
                    className="w-full px-2 py-1.5 rounded border border-gray-200 text-[13px] text-right bg-white focus:border-primary outline-none"
                  />
                </td>
                <td className="px-3 py-1.5 text-right font-semibold text-gray-900">
                  {(item.quantity * item.unitPrice).toLocaleString()}
                </td>
                <td className="px-3 py-1.5">
                  <input
                    value={item.optionText || ''}
                    onChange={e => updateItem(idx, 'optionText', e.target.value)}
                    placeholder="옵션"
                    className="w-full px-2 py-1.5 rounded border border-gray-200 text-[13px] bg-white focus:border-primary outline-none"
                  />
                </td>
                <td className="px-3 py-1.5 text-center">
                  {items.length > 1 && (
                    <button onClick={() => removeItem(idx)} className="text-red-400 hover:text-red-600 transition-colors">
                      <i className="fas fa-trash-alt text-xs"></i>
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <button onClick={addItem} className="text-[13px] text-primary font-semibold hover:underline mt-2 ml-3 mb-2">
        <i className="fas fa-plus mr-1"></i> 품목 추가
      </button>
    </div>
  );

  return (
    <div className="flex-1 flex flex-col">
      {!embedded && <Topbar title="거래명세표" breadcrumb="거래명세표 관리" />}
      <div className="flex-1 p-4 sm:p-6 overflow-y-auto">
        {/* Page Header */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-5 sm:mb-6">
          <div>
            <h2 className="text-base sm:text-lg font-bold text-gray-900 mb-0.5">거래명세표 관리</h2>
            <p className="text-xs sm:text-[13px] text-gray-500">거래명세표 등록, 조회 및 상태 관리</p>
          </div>
          <button onClick={() => { resetCreateForm(); setShowCreateModal(true); }} className="btn-primary self-start">
            <i className="fas fa-plus"></i> 신규 거래명세표
          </button>
        </div>

        {/* Filter Card */}
        <div className="card p-4 sm:p-5 mb-4 sm:mb-5">
          <div className="filter-card-header">
            <h3><i className="fas fa-filter" /> 검색 필터</h3>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 mb-4">
            <div>
              <label className="form-label">상태</label>
              <select
                value={filterState}
                onChange={e => setFilterState(e.target.value)}
                className="form-input"
              >
                <option value="all">전체</option>
                <option value="0">작성중</option>
                <option value="1">전송됨</option>
                <option value="2">수신됨</option>
                <option value="3">수정요청</option>
                <option value="4">확정</option>
                <option value="5">대체됨</option>
                <option value="6">취소</option>
              </select>
            </div>
            <div>
              <label className="form-label">거래처</label>
              <input
                value={searchKeyword}
                onChange={e => setSearchKeyword(e.target.value)}
                placeholder="거래처명 검색"
                className="form-input"
              />
            </div>
            <div>
              <label className="form-label">거래일 (시작)</label>
              <input type="date" className="form-input" />
            </div>
            <div>
              <label className="form-label">거래일 (종료)</label>
              <input type="date" className="form-input" />
            </div>
          </div>
          <div className="flex justify-end gap-2">
            <button onClick={() => { setFilterState('all'); setSearchKeyword(''); setCurrentPage(1); }} className="btn-reset">초기화</button>
            <button className="btn-search"><i className="fas fa-search" /> 검색</button>
          </div>
        </div>

        {/* Table Count Bar */}
        <div className="table-count-bar">
          <div className="count">총 <strong>{filtered.length}</strong>건</div>
          <div className="actions">
            <button className="btn-util"><i className="fas fa-download" /> <span className="hidden sm:inline">엑셀 다운로드</span></button>
            <button className="btn-util"><i className="fas fa-print" /> <span className="hidden sm:inline">인쇄</span></button>
          </div>
        </div>

        {/* Table */}
        <div className="card overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full" style={{ minWidth: '700px' }}>
              <thead>
                <tr>
                  <th className="table-header">상태</th>
                  <th className="table-header">문서번호</th>
                  <th className="table-header">발신</th>
                  <th className="table-header">수신</th>
                  <th className="table-header">거래일</th>
                  <th className="table-header text-right">금액</th>
                  <th className="table-header text-center">버전</th>
                  <th className="table-header">관리</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr><td colSpan={8} className="px-5 py-10 text-center text-gray-400 text-[13px]">로딩 중...</td></tr>
                ) : paginated.length === 0 ? (
                  <tr><td colSpan={8} className="px-5 py-10 text-center text-gray-400 text-[13px]">거래명세표가 없습니다.</td></tr>
                ) : paginated.map(doc => (
                  <tr key={doc.documentId} className="hover:bg-gray-50/50 transition-colors">
                    <td className="table-cell">{badge(doc.state)}</td>
                    <td
                      className="table-cell font-semibold text-primary cursor-pointer hover:underline"
                      onClick={() => openDetail(doc)}
                    >
                      {doc.documentId.substring(0, 8).toUpperCase()}
                    </td>
                    <td className="table-cell">{getCompanyName(doc.fromCompanyId)}</td>
                    <td className="table-cell">{getCompanyName(doc.toCompanyId)}</td>
                    <td className="table-cell">{new Date(doc.transactionDate).toLocaleDateString('ko-KR')}</td>
                    <td className="table-cell font-semibold text-gray-900 text-right">{doc.totalAmount.toLocaleString()}원</td>
                    <td className="table-cell text-gray-500 text-center">v{doc.versionNumber}</td>
                    <td className="table-cell">
                      <div className="flex items-center gap-1">
                        {doc.state === DocumentState.Draft && (
                          <button
                            onClick={() => handleSendClick(doc.documentId)}
                            className="px-2.5 py-1 rounded text-[11px] font-semibold text-white bg-primary hover:bg-primary-light transition-colors"
                          >
                            전송
                          </button>
                        )}
                        <button
                          onClick={() => openDetail(doc)}
                          className="w-7 h-7 rounded-lg flex items-center justify-center cursor-pointer text-gray-400 hover:bg-gray-100 hover:text-primary transition-colors"
                        >
                          <i className="fas fa-eye text-xs"></i>
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {/* Pagination */}
          <div className="pagination flex-col sm:flex-row gap-2">
            <div className="info">{(currentPage - 1) * PAGE_SIZE + 1}-{Math.min(currentPage * PAGE_SIZE, filtered.length)} / 총 {filtered.length}건</div>
            <div className="pages">
              <button className="page-btn" onClick={() => setCurrentPage(p => Math.max(1, p - 1))} disabled={currentPage === 1}><i className="fas fa-chevron-left" /></button>
              {Array.from({ length: totalPages }, (_, i) => i + 1).slice(0, 5).map(p => (
                <button key={p} className={`page-btn ${p === currentPage ? 'active' : ''}`} onClick={() => setCurrentPage(p)}>{p}</button>
              ))}
              <button className="page-btn" onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))} disabled={currentPage === totalPages}><i className="fas fa-chevron-right" /></button>
            </div>
          </div>
        </div>
      </div>

      {/* ═══════ Create Modal ═══════ */}
      {showCreateModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="bg-white rounded-xl shadow-2xl w-full max-w-[760px] max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between px-4 sm:px-6 py-4 border-b border-gray-100">
              <h3 className="text-[15px] font-bold text-gray-900">신규 거래명세표</h3>
              <button onClick={() => setShowCreateModal(false)} className="text-gray-400 hover:text-gray-600 transition-colors">
                <i className="fas fa-times"></i>
              </button>
            </div>
            <div className="p-4 sm:p-6 space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="form-label">수신 거래처</label>
                  <select
                    value={createForm.toCompanyId}
                    onChange={e => setCreateForm(f => ({ ...f, toCompanyId: e.target.value }))}
                    className="form-input"
                  >
                    <option value="">-- 선택 --</option>
                    {companies.filter(c => c.isActive).map(c => (
                      <option key={c.companyId} value={c.companyId}>{c.companyName}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="form-label">거래일</label>
                  <input
                    type="date"
                    value={createForm.transactionDate}
                    onChange={e => setCreateForm(f => ({ ...f, transactionDate: e.target.value }))}
                    className="form-input"
                  />
                </div>
              </div>
              <div>
                <label className="form-label">메모</label>
                <textarea
                  value={createForm.memo}
                  onChange={e => setCreateForm(f => ({ ...f, memo: e.target.value }))}
                  rows={2}
                  placeholder="비고 (선택)"
                  className="form-input resize-none"
                />
              </div>

              {/* Items */}
              <div>
                <label className="form-label mb-2">품목</label>
                <div className="border border-gray-200 rounded-lg overflow-hidden">
                  {renderItemTable(createForm.items, updateCreateItem, removeCreateItem, addCreateItem)}
                </div>
                <div className="flex justify-end mt-2.5">
                  <div className="text-[14px] font-bold text-gray-900">
                    합계: <span className="text-primary">{createItemsTotal.toLocaleString()}원</span>
                  </div>
                </div>
              </div>
            </div>
            <div className="flex justify-end gap-2 px-4 sm:px-6 py-4 border-t border-gray-100">
              <button onClick={() => setShowCreateModal(false)} className="px-4 py-2 rounded-lg bg-gray-100 text-gray-600 text-[13px] font-semibold hover:bg-gray-200 transition-colors">
                취소
              </button>
              <button onClick={handleCreate} className="px-4 py-2 rounded-lg bg-primary text-white text-[13px] font-semibold shadow-sm hover:bg-primary-light transition-colors">
                생성
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ═══════ Confirm Dialog ═══════ */}
      <ConfirmDialog
        open={confirmDialog.open}
        title="거래명세표 전송"
        message="이 거래명세표를 전송하시겠습니까?"
        confirmLabel="전송"
        onConfirm={handleSendConfirm}
        onCancel={() => setConfirmDialog({ open: false, docId: '' })}
      />

      {/* ═══════ Detail Modal ═══════ */}
      {showDetailModal && selectedDocument && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="bg-white rounded-xl shadow-2xl w-full max-w-[760px] max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between px-4 sm:px-6 py-4 border-b border-gray-100">
              <div className="flex items-center gap-3">
                <h3 className="text-[15px] font-bold text-gray-900">거래명세표 상세</h3>
                {badge(selectedDocument.state)}
              </div>
              <button onClick={() => setShowDetailModal(false)} className="text-gray-400 hover:text-gray-600 transition-colors">
                <i className="fas fa-times"></i>
              </button>
            </div>
            <div className="p-4 sm:p-6 space-y-4">
              {/* Document Info */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-[13px]">
                <div><span className="text-gray-500">문서번호:</span> <span className="font-semibold ml-2">{selectedDocument.documentId.substring(0, 8).toUpperCase()}</span></div>
                <div><span className="text-gray-500">버전:</span> <span className="font-semibold ml-2">v{selectedDocument.versionNumber}</span></div>
                <div><span className="text-gray-500">발신:</span> <span className="font-semibold ml-2">{getCompanyName(selectedDocument.fromCompanyId)}</span></div>
                <div><span className="text-gray-500">수신:</span> <span className="font-semibold ml-2">{getCompanyName(selectedDocument.toCompanyId)}</span></div>
                <div><span className="text-gray-500">거래일:</span> <span className="font-semibold ml-2">{new Date(selectedDocument.transactionDate).toLocaleDateString('ko-KR')}</span></div>
                <div><span className="text-gray-500">작성일:</span> <span className="font-semibold ml-2">{new Date(selectedDocument.createdAt).toLocaleDateString('ko-KR')}</span></div>
                {selectedDocument.memo && (
                  <div className="sm:col-span-2"><span className="text-gray-500">메모:</span> <span className="ml-2">{selectedDocument.memo}</span></div>
                )}
              </div>

              {/* Items */}
              <div>
                <div className="flex items-center justify-between mb-2">
                  <label className="text-[12px] font-semibold text-gray-600">품목 ({selectedDocument.items.length}건)</label>
                  {selectedDocument.state === DocumentState.Draft && !isEditingItems && (
                    <button onClick={startEditItems} className="text-[12px] text-primary font-semibold hover:underline">
                      <i className="fas fa-edit mr-1"></i>품목 수정
                    </button>
                  )}
                </div>

                {isEditingItems ? (
                  <div className="border border-primary/30 rounded-lg overflow-hidden bg-blue-50/30">
                    {renderItemTable(editItems, updateEditItem, removeEditItem, addEditItem)}
                    <div className="flex justify-between items-center px-3 py-2.5 border-t border-gray-200">
                      <div className="text-[13px] font-bold">
                        합계: <span className="text-primary">{editItems.reduce((s, i) => s + i.quantity * i.unitPrice, 0).toLocaleString()}원</span>
                      </div>
                      <div className="flex gap-2">
                        <button onClick={() => setIsEditingItems(false)} className="px-3 py-1.5 rounded-lg bg-gray-100 text-gray-600 text-[12px] font-semibold hover:bg-gray-200">취소</button>
                        <button onClick={saveEditItems} className="px-3 py-1.5 rounded-lg bg-primary text-white text-[12px] font-semibold hover:bg-primary-light">저장</button>
                      </div>
                    </div>
                  </div>
                ) : (
                  <div className="border border-gray-200 rounded-lg overflow-hidden">
                    <div className="overflow-x-auto">
                      <table className="w-full text-[13px]">
                        <thead className="bg-gray-50">
                          <tr>
                            <th className="px-3 py-2 text-left text-[11px] font-semibold text-gray-500 border-b">품목명</th>
                            <th className="px-3 py-2 text-right text-[11px] font-semibold text-gray-500 border-b">수량</th>
                            <th className="px-3 py-2 text-right text-[11px] font-semibold text-gray-500 border-b">단가</th>
                            <th className="px-3 py-2 text-right text-[11px] font-semibold text-gray-500 border-b">금액</th>
                            <th className="px-3 py-2 text-left text-[11px] font-semibold text-gray-500 border-b">비고</th>
                          </tr>
                        </thead>
                        <tbody>
                          {selectedDocument.items.map(item => (
                            <tr key={item.itemId} className="border-b border-gray-100">
                              <td className="px-3 py-2 text-gray-900">{item.itemName}</td>
                              <td className="px-3 py-2 text-right text-gray-700">{item.quantity}</td>
                              <td className="px-3 py-2 text-right text-gray-700">{item.unitPrice.toLocaleString()}</td>
                              <td className="px-3 py-2 text-right font-semibold text-gray-900">{item.lineAmount.toLocaleString()}</td>
                              <td className="px-3 py-2 text-gray-500">{item.optionText || '-'}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                    <div className="flex justify-end px-3 py-2.5 border-t border-gray-100">
                      <div className="text-[14px] font-bold text-gray-900">
                        합계: <span className="text-primary">{selectedDocument.totalAmount.toLocaleString()}원</span>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </div>

            {/* Detail Modal Footer */}
            <div className="flex justify-between px-4 sm:px-6 py-4 border-t border-gray-100">
              <div>
                {selectedDocument.state === DocumentState.Draft && (
                  <button onClick={() => handleSendClick(selectedDocument.documentId)} className="px-4 py-2 rounded-lg bg-primary text-white text-[13px] font-semibold shadow-sm hover:bg-primary-light transition-colors">
                    <i className="fas fa-paper-plane mr-1.5"></i>전송
                  </button>
                )}
              </div>
              <button onClick={() => setShowDetailModal(false)} className="px-4 py-2 rounded-lg bg-gray-100 text-gray-600 text-[13px] font-semibold hover:bg-gray-200 transition-colors">
                닫기
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
