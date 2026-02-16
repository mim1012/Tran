import { useEffect, useState, useRef } from 'react';
import apiClient from '../../services/api';
import type { Company } from '../../types';
import { useWorkspaceStore } from '../../stores/workspaceStore';

export default function CompanySelection() {
  const { recentCompanies, openCompany, loadRecentCompanies } = useWorkspaceStore();
  const [companies, setCompanies] = useState<Company[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchText, setSearchText] = useState('');
  const [selectedType, setSelectedType] = useState<string>('전체');
  const [currentPage, setCurrentPage] = useState(1);
  const searchRef = useRef<HTMLInputElement>(null);

  const PAGE_SIZE = 12;

  useEffect(() => {
    loadRecentCompanies();
    fetchCompanies();
  }, [loadRecentCompanies]);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'f') {
        e.preventDefault();
        searchRef.current?.focus();
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, []);

  const fetchCompanies = async () => {
    try {
      const res = await apiClient.get('/companies');
      setCompanies(res.data.data || []);
    } catch {
      setCompanies([
        { companyId: 'C001', companyName: '삼성전자', companyType: '고객사', businessNumber: '123-45-67890', address: '경기도 수원시 영통구', isActive: true, createdAt: '2026-01-01', representative: '이재용', phone: '031-200-1234' },
        { companyId: 'C002', companyName: 'LG화학', companyType: '공급사', businessNumber: '234-56-78901', address: '서울특별시 영등포구', isActive: true, createdAt: '2026-01-05', representative: '신학철', phone: '02-3773-1114' },
        { companyId: 'C003', companyName: 'SK하이닉스', companyType: '고객사', businessNumber: '345-67-89012', address: '경기도 이천시', isActive: true, createdAt: '2026-01-10', representative: '곽노정', phone: '031-630-4114' },
        { companyId: 'C004', companyName: '현대모비스', companyType: '공급사', businessNumber: '456-78-90123', address: '서울특별시 강남구', isActive: true, createdAt: '2026-01-15', representative: '정의선', phone: '02-2018-5114' },
        { companyId: 'C005', companyName: '포스코', companyType: '공급사', businessNumber: '567-89-01234', address: '경상북도 포항시', isActive: true, createdAt: '2026-01-20', representative: '최정우', phone: '054-220-0114' },
        { companyId: 'C006', companyName: '네이버', companyType: '고객사', businessNumber: '678-90-12345', address: '경기도 성남시 분당구', isActive: true, createdAt: '2026-02-01', representative: '최수연', phone: '1588-3820' },
        { companyId: 'C007', companyName: '카카오', companyType: '고객사', businessNumber: '789-01-23456', address: '제주특별자치도 제주시', isActive: true, createdAt: '2026-02-05', representative: '홍은택', phone: '1577-3754' },
        { companyId: 'C008', companyName: '한화솔루션', companyType: '공급사', businessNumber: '890-12-34567', address: '서울특별시 중구', isActive: true, createdAt: '2026-02-10', representative: '김동관', phone: '02-729-2700' },
      ]);
    } finally {
      setLoading(false);
    }
  };

  const filtered = companies.filter(c => {
    if (!c.isActive) return false;
    if (searchText) {
      const keyword = searchText.toLowerCase();
      const nameMatch = c.companyName.toLowerCase().includes(keyword);
      const bnMatch = c.businessNumber?.includes(keyword);
      const repMatch = c.representative?.toLowerCase().includes(keyword);
      if (!nameMatch && !bnMatch && !repMatch) return false;
    }
    if (selectedType !== '전체') {
      if (c.companyType !== selectedType) return false;
    }
    return true;
  });

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const paginated = filtered.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const handleSelect = (company: Company) => {
    openCompany(company);
  };

  const handleDelete = async (company: Company) => {
    if (!confirm(`'${company.companyName}'을(를) 삭제(비활성화)하시겠습니까?\n\n비활성화된 거래처는 목록에서 숨겨집니다.`)) return;
    try {
      await apiClient.delete(`/companies/${company.companyId}`);
      fetchCompanies();
    } catch {
      setCompanies(prev => prev.filter(c => c.companyId !== company.companyId));
    }
  };

  const handleReset = () => {
    setSearchText('');
    setSelectedType('전체');
    setCurrentPage(1);
  };

  return (
    <div className="flex-1 p-6 overflow-y-auto">
      {/* Page Header - 공통 구조 */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-6">
        <div>
          <h2 className="text-lg font-bold text-gray-900 mb-0.5">거래처 선택</h2>
          <p className="text-[13px] text-gray-500">작업할 거래처를 선택하면 해당 거래처의 작업 화면으로 이동합니다</p>
        </div>
        <button className="btn-primary self-start"><i className="fas fa-plus" /> 새 거래처 등록</button>
      </div>

      {/* 최근 거래처 카드 */}
      {recentCompanies.length > 0 && (
        <div className="card p-4 mb-5">
          <div className="flex items-center gap-2 mb-3">
            <i className="fas fa-clock text-primary" style={{ fontSize: '12px' }} />
            <span className="text-[13px] font-bold text-gray-900">최근 거래처</span>
          </div>
          <div className="flex items-center gap-2 flex-wrap">
            {recentCompanies.map(c => (
              <button
                key={c.companyId}
                onClick={() => handleSelect(c)}
                className="company-chip"
                style={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  gap: '6px',
                  padding: '6px 14px',
                  borderRadius: '20px',
                  border: '1px solid #D1D5DB',
                  background: '#fff',
                  color: '#2E4A7A',
                  fontSize: '13px',
                  fontWeight: 500,
                  cursor: 'pointer',
                  transition: 'all 0.15s ease',
                }}
              >
                <i className="fas fa-building" style={{ fontSize: '10px', opacity: 0.5 }} />
                {c.companyName}
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Filter Card - 공통 구조 */}
      <div className="card p-5 mb-5">
        <div className="filter-card-header">
          <h3><i className="fas fa-filter" /> 검색 필터</h3>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 mb-4">
          <div className="lg:col-span-2">
            <label className="form-label">거래처명 / 사업자번호</label>
            <input
              ref={searchRef}
              value={searchText}
              onChange={e => { setSearchText(e.target.value); setCurrentPage(1); }}
              placeholder="거래처명, 사업자번호 검색... (Ctrl+F)"
              className="form-input"
            />
          </div>
          <div>
            <label className="form-label">거래처 유형</label>
            <select
              value={selectedType}
              onChange={e => { setSelectedType(e.target.value); setCurrentPage(1); }}
              className="form-input"
            >
              <option value="전체">전체</option>
              <option value="고객사">고객사</option>
              <option value="공급사">공급사</option>
            </select>
          </div>
          <div className="flex items-end justify-end gap-2">
            <button onClick={handleReset} className="btn-reset">초기화</button>
            <button className="btn-search"><i className="fas fa-search" /> 검색</button>
          </div>
        </div>
      </div>

      {/* Table Count Bar - 공통 구조 */}
      <div className="table-count-bar">
        <div className="count">총 <strong>{filtered.length}</strong>개 거래처</div>
        <div className="actions">
          <button className="btn-util"><i className="fas fa-download" /> 엑셀 다운로드</button>
          <button className="btn-util"><i className="fas fa-print" /> 인쇄</button>
        </div>
      </div>

      {/* 거래처 카드 그리드 */}
      <div className="card p-5">
        {loading ? (
          <div className="text-center py-16 text-gray-400 text-sm">
            <i className="fas fa-spinner fa-spin mr-2" />
            거래처 목록을 불러오는 중...
          </div>
        ) : paginated.length === 0 ? (
          <div className="text-center py-16 text-gray-400">
            <i className="fas fa-search text-3xl mb-3 block opacity-40" />
            <p className="text-sm">검색 조건에 맞는 거래처가 없습니다</p>
          </div>
        ) : (
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
              gap: '16px',
            }}
          >
            {paginated.map(company => (
              <CompanyCard
                key={company.companyId}
                company={company}
                onSelect={handleSelect}
                onDelete={handleDelete}
              />
            ))}
          </div>
        )}

        {/* Pagination - 공통 구조 */}
        {!loading && filtered.length > 0 && (
          <div className="pagination" style={{ marginTop: '16px', borderTop: '1px solid #f3f4f6', paddingTop: '12px' }}>
            <div className="info">
              {(currentPage - 1) * PAGE_SIZE + 1}-{Math.min(currentPage * PAGE_SIZE, filtered.length)} / 총 {filtered.length}개
            </div>
            <div className="pages">
              <button
                className="page-btn"
                onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                disabled={currentPage === 1}
              >
                <i className="fas fa-chevron-left" />
              </button>
              {Array.from({ length: totalPages }, (_, i) => i + 1).slice(0, 5).map(p => (
                <button
                  key={p}
                  className={`page-btn ${p === currentPage ? 'active' : ''}`}
                  onClick={() => setCurrentPage(p)}
                >
                  {p}
                </button>
              ))}
              <button
                className="page-btn"
                onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
                disabled={currentPage === totalPages}
              >
                <i className="fas fa-chevron-right" />
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

// ─── CompanyCard 서브 컴포넌트 ───

interface CompanyCardProps {
  company: Company;
  onSelect: (company: Company) => void;
  onDelete: (company: Company) => void;
}

function CompanyCard({ company, onSelect, onDelete }: CompanyCardProps) {
  const typeBadge = company.companyType === '공급사'
    ? { cls: 'pending', label: '공급사' }
    : { cls: 'approved', label: company.companyType || '고객사' };

  return (
    <div
      className="company-card"
      style={{
        background: '#FEFEFE',
        borderRadius: '12px',
        padding: '20px',
        boxShadow: '0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04)',
        transition: 'all 0.2s ease',
        border: '1px solid #F3F4F6',
        cursor: 'pointer',
      }}
      onClick={() => onSelect(company)}
    >
      {/* 거래처명 */}
      <div className="flex items-center gap-2 mb-2">
        <div
          style={{
            width: '36px',
            height: '36px',
            borderRadius: '10px',
            background: 'linear-gradient(135deg, #2E4A7A, #4A6FA5)',
            color: '#fff',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontWeight: 700,
            fontSize: '14px',
            flexShrink: 0,
          }}
        >
          {company.companyName.charAt(0)}
        </div>
        <div className="flex-1 min-w-0">
          <div className="text-[15px] font-bold text-gray-900 truncate">
            {company.companyName}
          </div>
        </div>
      </div>

      {/* 타입 배지 - status-badge 공통 스타일 사용 */}
      <div className="mb-2.5">
        <span className={`status-badge ${typeBadge.cls}`}>
          <span className="w-1.5 h-1.5 rounded-full bg-current" />
          {typeBadge.label}
        </span>
      </div>

      {/* 정보 */}
      <div className="text-xs text-gray-500 leading-relaxed">
        <div>
          <span className="text-gray-400">사업자번호:</span>{' '}
          {company.businessNumber || '-'}
        </div>
        <div className="truncate">
          <span className="text-gray-400">주소:</span>{' '}
          {company.address || '-'}
        </div>
      </div>

      {/* 버튼 영역 */}
      <div
        className="flex gap-1.5 mt-3.5 pt-3.5"
        style={{ borderTop: '1px solid #F3F4F6' }}
        onClick={e => e.stopPropagation()}
      >
        <button
          onClick={() => onSelect(company)}
          className="btn-primary"
          style={{ flex: 1, justifyContent: 'center', padding: '7px 0', fontSize: '12.5px' }}
        >
          선택
        </button>
        <button
          onClick={() => {/* TODO: edit */}}
          className="w-8 h-8 rounded-lg flex items-center justify-center text-gray-400 hover:bg-blue-50 hover:text-primary transition-colors"
          style={{ border: '1px solid #E5E7EB', background: '#fff' }}
          title="수정"
        >
          <i className="fas fa-edit text-xs" />
        </button>
        <button
          onClick={() => onDelete(company)}
          className="w-8 h-8 rounded-lg flex items-center justify-center text-danger hover:bg-red-50 transition-colors"
          style={{ border: '1px solid #E5E7EB', background: '#fff' }}
          title="삭제(비활성화)"
        >
          <i className="fas fa-trash text-xs" />
        </button>
      </div>
    </div>
  );
}
