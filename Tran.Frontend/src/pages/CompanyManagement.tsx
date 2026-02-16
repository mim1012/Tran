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
      <div className="flex-1 p-7 overflow-y-auto">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-[22px] font-bold text-gray-900 mb-1">거래처 관리</h2>
            <p className="text-sm text-gray-500">거래처 등록, 조회 및 정보 관리</p>
          </div>
          <button className="btn-primary"><i className="fas fa-plus"></i> 거래처 등록</button>
        </div>

        <div className="card p-6 mb-5">
          <div className="grid grid-cols-4 gap-4">
            <div>
              <label className="block text-xs font-semibold text-gray-600 mb-1.5">거래처명 / 사업자번호</label>
              <input value={searchKeyword} onChange={e => setSearchKeyword(e.target.value)} placeholder="거래처명 또는 사업자번호 검색" className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none" />
            </div>
            <div>
              <label className="block text-xs font-semibold text-gray-600 mb-1.5">상태</label>
              <select className="w-full px-3.5 py-2.5 rounded-lg border border-gray-200 text-sm bg-gray-50 focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none">
                <option value="">전체</option>
                <option value="active">활성</option>
                <option value="inactive">비활성</option>
              </select>
            </div>
            <div className="col-span-2 flex items-end justify-end gap-2">
              <button onClick={() => setSearchKeyword('')} className="px-5 py-2.5 rounded-lg bg-gray-100 text-gray-600 text-sm font-semibold hover:bg-gray-200 transition-colors">초기화</button>
              <button className="px-5 py-2.5 rounded-lg bg-primary text-white text-sm font-semibold shadow hover:bg-primary-light transition-colors">검색</button>
            </div>
          </div>
        </div>

        <div className="flex items-center justify-between mb-3">
          <div className="text-sm text-gray-600">총 <strong className="text-primary font-bold">{filtered.length}</strong>건</div>
        </div>

        <div className="card overflow-hidden">
          <table className="w-full">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">거래처코드</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">거래처명</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">사업자번호</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">대표자</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">연락처</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">상태</th>
                <th className="px-5 py-3.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide border-b border-gray-200">관리</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400">로딩 중...</td></tr>
              ) : filtered.length === 0 ? (
                <tr><td colSpan={7} className="px-5 py-10 text-center text-gray-400">거래처 데이터가 없습니다.</td></tr>
              ) : filtered.map(company => (
                <tr key={company.companyId} className="hover:bg-gray-50 transition-colors">
                  <td className="px-5 py-3.5 text-sm font-semibold text-primary border-b border-gray-100">{company.companyId}</td>
                  <td className="px-5 py-3.5 text-sm text-gray-700 font-medium border-b border-gray-100">{company.companyName}</td>
                  <td className="px-5 py-3.5 text-sm text-gray-700 border-b border-gray-100">{company.businessNumber || '-'}</td>
                  <td className="px-5 py-3.5 text-sm text-gray-700 border-b border-gray-100">{company.representative || '-'}</td>
                  <td className="px-5 py-3.5 text-sm text-gray-700 border-b border-gray-100">{company.phone || '-'}</td>
                  <td className="px-5 py-3.5 text-sm border-b border-gray-100">
                    <span className={`status-badge ${company.isActive ? 'approved' : 'rejected'}`}>
                      <span className="w-1.5 h-1.5 rounded-full bg-current"></span>
                      {company.isActive ? '활성' : '비활성'}
                    </span>
                  </td>
                  <td className="px-5 py-3.5 text-sm border-b border-gray-100">
                    <div className="flex gap-1">
                      <button className="w-8 h-8 rounded-lg flex items-center justify-center text-gray-400 hover:bg-blue-50 hover:text-primary transition-colors"><i className="fas fa-edit text-xs"></i></button>
                      <button className="w-8 h-8 rounded-lg flex items-center justify-center text-gray-400 hover:bg-red-50 hover:text-danger transition-colors"><i className="fas fa-trash text-xs"></i></button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
