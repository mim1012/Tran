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
        // Confirmed 상태만 집계 대상
        var summaries = await _context.Documents
            .Where(d => d.State == DocumentState.Confirmed)
            .Where(d => d.TransactionDate >= fromDate && d.TransactionDate <= toDate)
            .GroupBy(d => d.ToCompanyId)
            .Select(g => new
            {
                CompanyId = g.Key,
                TotalCount = g.Count(),
                TotalAmount = g.Sum(x => x.TotalAmount),
                AverageAmount = g.Average(x => x.TotalAmount)
            })
            .ToListAsync();

        // Company 정보 조인
        var result = new List<SettlementSummary>();
        foreach (var summary in summaries)
        {
            var company = await _context.Companies
                .Where(c => c.CompanyId == summary.CompanyId)
                .FirstOrDefaultAsync();

            result.Add(new SettlementSummary
            {
                CompanyId = summary.CompanyId,
                CompanyName = company?.CompanyName ?? "(알 수 없음)",
                TotalCount = summary.TotalCount,
                TotalAmount = summary.TotalAmount,
                AverageAmount = summary.AverageAmount
            });
        }

        return result.OrderByDescending(x => x.TotalAmount).ToList();
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
