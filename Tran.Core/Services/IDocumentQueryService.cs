using Tran.Core.Models;

namespace Tran.Core.Services;

/// <summary>
/// 문서 조회 전용 서비스 인터페이스
/// Repository 패턴 - 읽기 전용 쿼리
/// </summary>
public interface IDocumentQueryService
{
    /// <summary>
    /// 특정 기간 동안의 확정된 문서를 거래처별로 집계
    /// </summary>
    Task<List<SettlementSummary>> GetSettlementSummariesAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// 특정 거래처의 확정된 문서 목록 조회
    /// </summary>
    Task<List<Document>> GetConfirmedDocumentsByCompanyAsync(string companyId, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// 거래처 정보 조회
    /// </summary>
    Task<Company?> GetCompanyByIdAsync(string companyId);
}

/// <summary>
/// 거래처별 정산 집계 DTO
/// </summary>
public class SettlementSummary
{
    public string CompanyId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageAmount { get; set; }
}
