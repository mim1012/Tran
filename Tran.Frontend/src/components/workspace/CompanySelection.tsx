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
  const searchRef = useRef<HTMLInputElement>(null);

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
      // fallback: 더미 데이터
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

  const handleSelect = (company: Company) => {
    openCompany(company);
  };

  const handleDelete = async (company: Company) => {
    if (!confirm(`'${company.companyName}'을(를) 삭제(비활성화)하시겠습니까?\n\n비활성화된 거래처는 목록에서 숨겨집니다.`)) return;
    try {
      await apiClient.delete(`/companies/${company.companyId}`);
      fetchCompanies();
    } catch {
      // fallback: 로컬에서 제거
      setCompanies(prev => prev.filter(c => c.companyId !== company.companyId));
    }
  };

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', background: '#F0F2F5', minHeight: '100vh' }}>
      {/* 헤더 영역 */}
      <div
        style={{
          background: 'linear-gradient(135deg, #2E4A7A 0%, #3B5998 50%, #4A6FA5 100%)',
          padding: '40px 20px',
          textAlign: 'center',
        }}
      >
        <h1 style={{ fontSize: '22px', fontWeight: 700, color: '#fff', margin: 0 }}>
          작업할 거래처를 선택하세요
        </h1>
        <p style={{ fontSize: '13px', color: 'rgba(255,255,255,0.6)', marginTop: '10px' }}>
          거래처를 선택하면 해당 거래처와의 작업 화면으로 이동합니다
        </p>
      </div>

      {/* 최근 거래처 칩 영역 */}
      {recentCompanies.length > 0 && (
        <div
          style={{
            background: '#F9FAFB',
            padding: '14px 24px',
            borderBottom: '1px solid #E5E7EB',
            display: 'flex',
            alignItems: 'center',
            gap: '10px',
            flexWrap: 'wrap',
          }}
        >
          <span style={{ fontSize: '12px', fontWeight: 600, color: '#6B7280', marginRight: '4px' }}>
            최근 거래처
          </span>
          {recentCompanies.map(c => (
            <button
              key={c.companyId}
              onClick={() => handleSelect(c)}
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
              onMouseEnter={e => {
                e.currentTarget.style.background = '#EBF0F7';
                e.currentTarget.style.borderColor = '#2E4A7A';
              }}
              onMouseLeave={e => {
                e.currentTarget.style.background = '#fff';
                e.currentTarget.style.borderColor = '#D1D5DB';
              }}
            >
              <i className="fas fa-clock" style={{ fontSize: '10px', opacity: 0.5 }} />
              {c.companyName}
            </button>
          ))}
        </div>
      )}

      {/* 검색 및 필터 영역 */}
      <div
        style={{
          padding: '16px 24px',
          background: '#fff',
          borderBottom: '1px solid #E5E7EB',
          display: 'flex',
          alignItems: 'center',
          gap: '12px',
          flexWrap: 'wrap',
        }}
      >
        <div
          style={{
            flex: 1,
            minWidth: '200px',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            background: '#F9FAFB',
            borderRadius: '8px',
            padding: '0 12px',
            border: '1px solid #E5E7EB',
            transition: 'border-color 0.15s',
          }}
        >
          <i className="fas fa-search" style={{ color: '#9CA3AF', fontSize: '13px' }} />
          <input
            ref={searchRef}
            type="text"
            value={searchText}
            onChange={e => setSearchText(e.target.value)}
            placeholder="거래처명, 사업자번호 검색... (Ctrl+F)"
            style={{
              flex: 1,
              border: 'none',
              outline: 'none',
              background: 'transparent',
              padding: '10px 0',
              fontSize: '14px',
              color: '#333',
            }}
          />
          {searchText && (
            <button
              onClick={() => setSearchText('')}
              style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#9CA3AF', fontSize: '12px' }}
            >
              <i className="fas fa-times" />
            </button>
          )}
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span style={{ fontSize: '13px', color: '#6B7280', fontWeight: 500 }}>타입:</span>
          <select
            value={selectedType}
            onChange={e => setSelectedType(e.target.value)}
            className="form-input"
            style={{ width: '140px', padding: '8px 12px' }}
          >
            <option value="전체">전체</option>
            <option value="고객사">고객사</option>
            <option value="공급사">공급사</option>
          </select>
        </div>
      </div>

      {/* 거래처 카드 그리드 */}
      <div style={{ flex: 1, padding: '24px', overflowY: 'auto' }}>
        {loading ? (
          <div style={{ textAlign: 'center', padding: '60px 0', color: '#9CA3AF', fontSize: '14px' }}>
            <i className="fas fa-spinner fa-spin" style={{ marginRight: '8px' }} />
            거래처 목록을 불러오는 중...
          </div>
        ) : filtered.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '60px 0', color: '#9CA3AF' }}>
            <i className="fas fa-search" style={{ fontSize: '32px', marginBottom: '12px', display: 'block', opacity: 0.4 }} />
            <p style={{ fontSize: '14px' }}>검색 조건에 맞는 거래처가 없습니다</p>
          </div>
        ) : (
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
              gap: '16px',
            }}
          >
            {filtered.map(company => (
              <CompanyCard
                key={company.companyId}
                company={company}
                onSelect={handleSelect}
                onDelete={handleDelete}
              />
            ))}
          </div>
        )}
      </div>

      {/* 하단 바 */}
      <div
        style={{
          padding: '14px 24px',
          background: '#fff',
          borderTop: '1px solid #E5E7EB',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
        }}
      >
        <div style={{ display: 'flex', gap: '10px' }}>
          <button className="btn-primary" style={{ padding: '8px 20px' }}>
            <i className="fas fa-plus" /> 새 거래처 등록
          </button>
        </div>
        <span style={{ fontSize: '13px', color: '#6B7280' }}>
          총 <strong style={{ color: '#2E4A7A' }}>{filtered.length}</strong>개 거래처
        </span>
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
  const [hovered, setHovered] = useState(false);

  const typeBadge = company.companyType === '공급사'
    ? { bg: '#FFF3E0', color: '#F39C12', label: '공급사' }
    : { bg: '#E8F5E9', color: '#27AE60', label: company.companyType || '고객사' };

  return (
    <div
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
      style={{
        background: '#fff',
        borderRadius: '12px',
        padding: '20px',
        boxShadow: hovered
          ? '0 4px 16px rgba(46, 74, 122, 0.12)'
          : '0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04)',
        transition: 'all 0.2s ease',
        transform: hovered ? 'translateY(-2px)' : 'none',
        border: hovered ? '1px solid #4A6FA5' : '1px solid transparent',
        cursor: 'pointer',
      }}
      onClick={() => onSelect(company)}
    >
      {/* 거래처명 */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '8px' }}>
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
        <div style={{ flex: 1, minWidth: 0 }}>
          <div
            style={{
              fontSize: '15px',
              fontWeight: 700,
              color: '#111827',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}
          >
            {company.companyName}
          </div>
        </div>
      </div>

      {/* 타입 배지 */}
      <div style={{ marginBottom: '10px' }}>
        <span
          style={{
            display: 'inline-block',
            padding: '3px 10px',
            borderRadius: '4px',
            background: typeBadge.bg,
            color: typeBadge.color,
            fontSize: '11px',
            fontWeight: 600,
          }}
        >
          {typeBadge.label}
        </span>
      </div>

      {/* 정보 */}
      <div style={{ fontSize: '12px', color: '#6B7280', lineHeight: 1.8 }}>
        <div>
          <span style={{ color: '#9CA3AF' }}>사업자번호:</span>{' '}
          {company.businessNumber || '-'}
        </div>
        <div
          style={{
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
          }}
        >
          <span style={{ color: '#9CA3AF' }}>주소:</span>{' '}
          {company.address || '-'}
        </div>
      </div>

      {/* 버튼 영역 */}
      <div
        style={{
          display: 'flex',
          gap: '6px',
          marginTop: '14px',
          paddingTop: '14px',
          borderTop: '1px solid #F3F4F6',
        }}
        onClick={e => e.stopPropagation()}
      >
        <button
          onClick={() => onSelect(company)}
          style={{
            flex: 1,
            padding: '8px 0',
            borderRadius: '8px',
            background: 'linear-gradient(135deg, #2E4A7A, #4A6FA5)',
            color: '#fff',
            fontSize: '13px',
            fontWeight: 600,
            border: 'none',
            cursor: 'pointer',
            transition: 'all 0.15s ease',
          }}
        >
          선택
        </button>
        <button
          onClick={() => {/* TODO: edit */}}
          style={{
            width: '34px',
            height: '34px',
            borderRadius: '8px',
            border: '1px solid #E5E7EB',
            background: '#fff',
            color: '#6B7280',
            fontSize: '13px',
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            transition: 'all 0.15s ease',
          }}
          title="수정"
        >
          <i className="fas fa-edit" />
        </button>
        <button
          onClick={() => onDelete(company)}
          style={{
            width: '34px',
            height: '34px',
            borderRadius: '8px',
            border: '1px solid #E5E7EB',
            background: '#fff',
            color: '#E74C3C',
            fontSize: '13px',
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            transition: 'all 0.15s ease',
          }}
          title="삭제(비활성화)"
        >
          <i className="fas fa-trash" />
        </button>
      </div>
    </div>
  );
}
