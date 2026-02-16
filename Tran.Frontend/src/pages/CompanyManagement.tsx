import { useEffect, useState } from 'react';
import Topbar from '../components/layout/Topbar';
import apiClient from '../services/api';
import type { Company } from '../types';

export default function CompanyManagement() {
  const [companies, setCompanies] = useState<Company[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchKeyword, setSearchKeyword] = useState('');

  useEffect(() => { fetchCompanies(); }, []);

  const fetchCompanies = async () => {
    try {
      const res = await apiClient.get('/companies');
      setCompanies(res.data.data || []);
    } catch { /* fallback */ } finally { setLoading(false); }
  };

  const filtered = companies.filter(c => {
    if (searchKeyword && !c.companyName.includes(searchKeyword) && !c.businessNumber?.includes(searchKeyword)) return false;
    return true;
  });

  return (
    <div className="flex-1 flex flex-col">
      <Topbar title="거래처 관리" breadcrumb="거래처 관리" />
      <div className="flex-1 p-4 sm:p-6 overflow-y-auto">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-5">
          <div>
            <h2 className="text-lg font-bold text-gray-900 mb-0.5">거래처 관리</h2>
            <p className="text-[13px] text-gray-500">거래처 등록, 조회 및 정보 관리</p>
          </div>
          <button className="btn-primary self-start"><i className="fas fa-plus"></i> 거래처 등록</button>
        </div>

        <div className="card p-4 sm:p-5 mb-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
            <div>
              <label className="form-label">거래처명 / 사업자번호</label>
              <input value={searchKeyword} onChange={e => setSearchKeyword(e.target.value)} placeholder="거래처명 또는 사업자번호 검색" className="form-input" />
            </div>
            <div>
              <label className="form-label">상태</label>
              <select className="form-input">
                <option value="">전체</option>
                <option value="active">활성</option>
                <option value="inactive">비활성</option>
              </select>
            </div>
            <div className="lg:col-span-2 flex items-end justify-end gap-2">
              <button onClick={() => setSearchKeyword('')} className="px-4 py-2 rounded-lg bg-gray-100 text-gray-600 text-[13px] font-semibold hover:bg-gray-200 transition-colors">초기화</button>
              <button className="px-4 py-2 rounded-lg bg-primary text-white text-[13px] font-semibold shadow-sm hover:bg-primary-light transition-colors">검색</button>
            </div>
          </div>
        </div>

        <div className="flex items-center justify-between mb-2.5">
          <div className="text-[13px] text-gray-600">총 <strong className="text-primary font-bold">{filtered.length}</strong>건</div>
        </div>

        <div className="card overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="bg-gray-50">
                  <th className="table-header">거래처코드</th>
                  <th className="table-header">거래처명</th>
                  <th className="table-header">사업자번호</th>
                  <th className="table-header">대표자</th>
                  <th className="table-header">연락처</th>
                  <th className="table-header">상태</th>
                  <th className="table-header">관리</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400 text-[13px]">로딩 중...</td></tr>
                ) : filtered.length === 0 ? (
                  <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400 text-[13px]">거래처 데이터가 없습니다.</td></tr>
                ) : filtered.map(company => (
                  <tr key={company.companyId} className="hover:bg-gray-50/50 transition-colors">
                    <td className="table-cell font-semibold text-primary">{company.companyId}</td>
                    <td className="table-cell font-medium">{company.companyName}</td>
                    <td className="table-cell">{company.businessNumber || '-'}</td>
                    <td className="table-cell">{company.representative || '-'}</td>
                    <td className="table-cell">{company.phone || '-'}</td>
                    <td className="table-cell">
                      <span className={`status-badge ${company.isActive ? 'approved' : 'rejected'}`}>
                        <span className="w-1.5 h-1.5 rounded-full bg-current"></span>
                        {company.isActive ? '활성' : '비활성'}
                      </span>
                    </td>
                    <td className="table-cell">
                      <div className="flex gap-1">
                        <button className="w-7 h-7 rounded-lg flex items-center justify-center text-gray-400 hover:bg-blue-50 hover:text-primary transition-colors"><i className="fas fa-edit text-xs"></i></button>
                        <button className="w-7 h-7 rounded-lg flex items-center justify-center text-gray-400 hover:bg-red-50 hover:text-danger transition-colors"><i className="fas fa-trash text-xs"></i></button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
}
