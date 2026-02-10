using Tran.Core.Models;

namespace Tran.Core.Services;

/// <summary>
/// 판매 관리 서비스 인터페이스
/// 판매 CRUD 및 상태 전이 (Draft → Confirmed → 재고 출고)
/// </summary>
public interface ISaleService
{
    /// <summary>
    /// 거래처별 판매 목록 조회
    /// </summary>
    Task<List<Sale>> GetSalesByCompanyAsync(string companyId);

    /// <summary>
    /// 판매 ID로 조회 (Items 포함)
    /// </summary>
    Task<Sale?> GetByIdAsync(int saleId);

    /// <summary>
    /// 판매 초안 생성 (Draft 상태)
    /// </summary>
    Task<Sale> CreateDraftAsync(Sale sale);

    /// <summary>
    /// 판매 확정 → 재고 출고 처리 (Inventory outbound)
    /// </summary>
    Task<Sale> ConfirmSaleAsync(int saleId);

    /// <summary>
    /// 판매 취소
    /// </summary>
    Task CancelSaleAsync(int saleId);
}
