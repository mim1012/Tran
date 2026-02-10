using Tran.Core.Models;

namespace Tran.Core.Services;

/// <summary>
/// 입고(구매) 관리 서비스 인터페이스
/// 입고 확인 및 재고 반영
/// </summary>
public interface IPurchaseService
{
    /// <summary>
    /// 거래처별 입고 목록 조회
    /// </summary>
    Task<List<Purchase>> GetPurchasesByCompanyAsync(string companyId);

    /// <summary>
    /// 입고 ID로 조회 (Items 포함)
    /// </summary>
    Task<Purchase?> GetByIdAsync(int purchaseId);

    /// <summary>
    /// 입고 확인 처리 → 재고 입고 반영 (Inventory inbound)
    /// 수량 입력 후 Delivered 또는 PartiallyDelivered 상태 설정
    /// </summary>
    /// <param name="purchaseId">입고 ID</param>
    /// <param name="receivedItems">각 품목별 수령/불량 수량</param>
    Task<Purchase> ConfirmDeliveryAsync(int purchaseId, List<(int PurchaseItemId, decimal ReceivedQty, decimal DefectQty)> receivedItems);
}
