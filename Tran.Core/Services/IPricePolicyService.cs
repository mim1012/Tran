using Tran.Core.Models;

namespace Tran.Core.Services;

/// <summary>
/// 단가 정책 서비스 인터페이스
/// 거래처별 단가 관리, 가격 이력, 거래처-품목 연결 관리
/// </summary>
public interface IPricePolicyService
{
    /// <summary>
    /// 거래처-품목 단가 조회 (null이면 미등록)
    /// </summary>
    Task<decimal?> GetCompanyPriceAsync(string companyId, int productId);

    /// <summary>
    /// 거래처-품목 단가 등록/변경 (PriceHistory 자동 생성)
    /// </summary>
    Task UpdateCompanyPriceAsync(string companyId, int productId, decimal newPrice, string? reason = null);

    /// <summary>
    /// 특정 거래처의 전체 단가 목록
    /// </summary>
    Task<List<CompanyPrice>> GetCompanyPricesAsync(string companyId);

    /// <summary>
    /// 특정 거래처-품목의 단가 변경 이력
    /// </summary>
    Task<List<PriceHistory>> GetPriceHistoryAsync(string companyId, int productId);

    /// <summary>
    /// 거래처-품목 연결 등록
    /// </summary>
    Task RegisterCompanyProductAsync(string companyId, int productId);

    /// <summary>
    /// 특정 거래처의 거래 품목 목록
    /// </summary>
    Task<List<CompanyProduct>> GetCompanyProductsAsync(string companyId);
}
