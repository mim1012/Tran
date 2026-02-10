using Tran.Core.Models;

namespace Tran.Core.Services;

/// <summary>
/// 재고 관리 서비스 인터페이스
/// 재고 현황 조회, 입출고 처리, 재고 조정
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// 전체 재고 현황 조회
    /// </summary>
    Task<List<Inventory>> GetAllInventoryAsync();

    /// <summary>
    /// 품목별 재고 조회
    /// </summary>
    Task<Inventory?> GetByProductIdAsync(int productId);

    /// <summary>
    /// 재고 이동 이력 조회
    /// </summary>
    /// <param name="productId">품목 ID (null이면 전체)</param>
    /// <param name="limit">최대 조회 건수</param>
    Task<List<InventoryTransaction>> GetTransactionHistoryAsync(int? productId = null, int limit = 100);

    /// <summary>
    /// 재고 수동 조정 (실사 등)
    /// </summary>
    Task AdjustInventoryAsync(int productId, decimal adjustmentQty, string reason);

    // ─── 내부 자동화 메서드 (다른 서비스에서 호출) ───

    /// <summary>
    /// 입고 처리: ConfirmedQuantity 증가 + 트랜잭션 로그
    /// </summary>
    Task ProcessInboundAsync(int productId, decimal quantity, string referenceType, int referenceId);

    /// <summary>
    /// 출고 처리: ConfirmedQuantity 감소 + 트랜잭션 로그
    /// </summary>
    Task ProcessOutboundAsync(int productId, decimal quantity, string referenceType, int referenceId);

    /// <summary>
    /// 입고 예정 수량 추가 (발주 확정 시)
    /// </summary>
    Task AddPendingInboundAsync(int productId, decimal quantity);

    /// <summary>
    /// 입고 예정 수량 제거 (입고 확인 시)
    /// </summary>
    Task RemovePendingInboundAsync(int productId, decimal quantity);
}
