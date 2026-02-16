import { create } from 'zustand';
import type { Company, CompanyWorkspace, DetailViewType } from '../types';

/** 기능별 탭 정의 */
export const FUNCTION_TABS = [
  { id: 'orders', label: '발주', icon: 'fa-file-invoice' },
  { id: 'purchases', label: '구매', icon: 'fa-truck' },
  { id: 'sales', label: '판매', icon: 'fa-shopping-cart' },
  { id: 'inventory', label: '재고', icon: 'fa-warehouse' },
  { id: 'products', label: '품목', icon: 'fa-boxes-stacked' },
  { id: 'quotations', label: '견적', icon: 'fa-file-alt' },
  { id: 'documents', label: '거래명세표', icon: 'fa-file-invoice-dollar' },
] as const;

export type FunctionTabId = (typeof FUNCTION_TABS)[number]['id'];

interface WorkspaceState {
  /** 열린 거래처 워크스페이스 목록 */
  workspaces: CompanyWorkspace[];
  /** 현재 활성 거래처 ID (null이면 거래처 선택 화면) */
  activeCompanyId: string | null;
  /** 최근 거래처 목록 (최대 5개) */
  recentCompanies: Company[];
  /** 거래처 선택 화면 표시 여부 */
  showCompanySelection: boolean;

  // Actions
  /** 거래처 워크스페이스 열기 (이미 열려있으면 해당 탭으로 전환) */
  openCompany: (company: Company) => void;
  /** 거래처 워크스페이스 닫기 */
  closeCompany: (companyId: string) => void;
  /** 활성 거래처 전환 */
  setActiveCompany: (companyId: string) => void;
  /** 기능 탭 전환 */
  setFunctionTab: (companyId: string, tabId: string) => void;
  /** 상세 뷰 전환 */
  setDetailView: (companyId: string, view: DetailViewType, detailId?: string | number) => void;
  /** 거래처 선택 화면 표시/숨김 */
  setShowCompanySelection: (show: boolean) => void;
  /** 최근 거래처 목록에 추가 */
  addToRecent: (company: Company) => void;
  /** 최근 거래처 목록 초기화 (localStorage에서 로드) */
  loadRecentCompanies: () => void;
}

const RECENT_COMPANIES_KEY = 'tran_recent_companies';

export const useWorkspaceStore = create<WorkspaceState>((set, get) => ({
  workspaces: [],
  activeCompanyId: null,
  recentCompanies: [],
  showCompanySelection: true,

  openCompany: (company: Company) => {
    const { workspaces, addToRecent } = get();
    const existing = workspaces.find(w => w.company.companyId === company.companyId);

    if (existing) {
      set({ activeCompanyId: company.companyId, showCompanySelection: false });
    } else {
      const newWorkspace: CompanyWorkspace = {
        company,
        activeFunctionTab: 'orders',
        detailView: 'list',
      };
      set({
        workspaces: [...workspaces, newWorkspace],
        activeCompanyId: company.companyId,
        showCompanySelection: false,
      });
    }

    addToRecent(company);
  },

  closeCompany: (companyId: string) => {
    const { workspaces, activeCompanyId } = get();
    const idx = workspaces.findIndex(w => w.company.companyId === companyId);
    const newWorkspaces = workspaces.filter(w => w.company.companyId !== companyId);

    let newActiveId = activeCompanyId;
    if (activeCompanyId === companyId) {
      if (newWorkspaces.length > 0) {
        const newIdx = Math.min(idx, newWorkspaces.length - 1);
        newActiveId = newWorkspaces[newIdx].company.companyId;
      } else {
        newActiveId = null;
      }
    }

    set({
      workspaces: newWorkspaces,
      activeCompanyId: newActiveId,
      showCompanySelection: newWorkspaces.length === 0,
    });
  },

  setActiveCompany: (companyId: string) => {
    set({ activeCompanyId: companyId, showCompanySelection: false });
  },

  setFunctionTab: (companyId: string, tabId: string) => {
    set(state => ({
      workspaces: state.workspaces.map(w =>
        w.company.companyId === companyId
          ? { ...w, activeFunctionTab: tabId, detailView: 'list' as DetailViewType, detailId: undefined }
          : w
      ),
    }));
  },

  setDetailView: (companyId: string, view: DetailViewType, detailId?: string | number) => {
    set(state => ({
      workspaces: state.workspaces.map(w =>
        w.company.companyId === companyId
          ? { ...w, detailView: view, detailId }
          : w
      ),
    }));
  },

  setShowCompanySelection: (show: boolean) => {
    set({ showCompanySelection: show });
  },

  addToRecent: (company: Company) => {
    const { recentCompanies } = get();
    const filtered = recentCompanies.filter(c => c.companyId !== company.companyId);
    const updated = [company, ...filtered].slice(0, 5);
    set({ recentCompanies: updated });
    try {
      localStorage.setItem(RECENT_COMPANIES_KEY, JSON.stringify(updated));
    } catch {
      // ignore
    }
  },

  loadRecentCompanies: () => {
    try {
      const stored = localStorage.getItem(RECENT_COMPANIES_KEY);
      if (stored) {
        const parsed = JSON.parse(stored) as Company[];
        set({ recentCompanies: parsed.slice(0, 5) });
      }
    } catch {
      // ignore
    }
  },
}));
