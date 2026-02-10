using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Data.Services;

/// <summary>
/// 문서 조회 전용 서비스 구현
/// Repository 패턴 - EF Core를 캡슐화하여 ViewModel이 DbContext에 직접 접근하지 않도록 함
/// </summary>
public class DocumentQueryService : IDocumentQueryService
{
    private readonly TranDbContext _context;

    public DocumentQueryService(TranDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 특정 기간 동안의 확정된 문서를 거래처별로 집계
    /// 정산 관리 화면의 메인 데이터 소스
    /// </summary>
    public async Task<List<SettlementSummary>> GetSettlementSummariesAsync(DateTime fromDate, DateTime toDate)
    {
        // Confirmed 상태만 집계 대상 - Join으로 N+1 쿼리 방지
        var result = await _context.Documents
            .Where(d => d.State == DocumentState.Confirmed)
            .Where(d => d.TransactionDate >= fromDate && d.TransactionDate <= toDate)
            .Join(
                _context.Companies,
                d => d.ToCompanyId,
                c => c.CompanyId,
                (d, c) => new { Document = d, Company = c })
            .GroupBy(x => new { x.Company.CompanyId, x.Company.CompanyName })
            .Select(g => new SettlementSummary
            {
                CompanyId = g.Key.CompanyId,
                CompanyName = g.Key.CompanyName,
                TotalCount = g.Count(),
                TotalAmount = g.Sum(x => x.Document.TotalAmount),
                AverageAmount = g.Average(x => x.Document.TotalAmount)
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToListAsync();

        return result;
    }

    /// <summary>
    /// 특정 거래처의 확정된 문서 목록 조회
    /// 집계 행 선택 시 상세 목록 표시용
    /// </summary>
    public async Task<List<Document>> GetConfirmedDocumentsByCompanyAsync(
        string companyId,
        DateTime fromDate,
        DateTime toDate)
    {
        return await _context.Documents
            .Where(d => d.State == DocumentState.Confirmed)
            .Where(d => d.ToCompanyId == companyId)
            .Where(d => d.TransactionDate >= fromDate && d.TransactionDate <= toDate)
            .OrderByDescending(d => d.TransactionDate)
            .ToListAsync();
    }

    /// <summary>
    /// 거래처 정보 조회
    /// </summary>
    public async Task<Company?> GetCompanyByIdAsync(string companyId)
    {
        return await _context.Companies
            .Where(c => c.CompanyId == companyId)
            .FirstOrDefaultAsync();
    }
}
