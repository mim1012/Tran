using Tran.Core.Models;

namespace Tran.Core.Services;

/// <summary>
/// 품목 관리 서비스 인터페이스
/// 품목 CRUD 및 검색
/// </summary>
public interface IProductService
{
    /// <summary>
    /// 전체 품목 목록 조회
    /// </summary>
    /// <param name="includeInactive">비활성 품목 포함 여부</param>
    Task<List<Product>> GetAllProductsAsync(bool includeInactive = false);

    /// <summary>
    /// 품목명/품목코드 키워드 검색
    /// </summary>
    Task<List<Product>> SearchProductsAsync(string keyword);

    /// <summary>
    /// 품목 ID로 조회
    /// </summary>
    Task<Product?> GetByIdAsync(int productId);

    /// <summary>
    /// 새 품목 생성
    /// </summary>
    Task<Product> CreateAsync(Product product);

    /// <summary>
    /// 품목 정보 수정
    /// </summary>
    Task UpdateAsync(Product product);

    /// <summary>
    /// 전체 품목 + 연관 회사명 조회
    /// </summary>
    Task<List<ProductDisplayItem>> GetAllProductsWithCompaniesAsync(bool includeInactive = false);

    /// <summary>
    /// 특정 회사의 품목 조회
    /// </summary>
    Task<List<ProductDisplayItem>> GetProductsByCompanyAsync(string companyId, bool includeInactive = false);

    /// <summary>
    /// 키워드 검색 + 회사 필터
    /// </summary>
    Task<List<ProductDisplayItem>> SearchProductsWithCompaniesAsync(string keyword, string? companyId = null);
}
