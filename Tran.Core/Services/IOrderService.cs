using Tran.Core.Models;

namespace Tran.Core.Services;

/// <summary>
/// 발주 관리 서비스 인터페이스
/// 발주 CRUD 및 상태 전이 (Draft → Completed → Purchase 자동 생성)
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// 거래처별 발주 목록 조회
    /// </summary>
    Task<List<Order>> GetOrdersByCompanyAsync(string companyId);

    /// <summary>
    /// 발주 ID로 조회 (Items 포함)
    /// </summary>
    Task<Order?> GetByIdAsync(int orderId);

    /// <summary>
    /// 발주 초안 생성 (Draft 상태)
    /// </summary>
    Task<Order> CreateDraftAsync(Order order);

    /// <summary>
    /// 발주 확정 → Purchase 자동 생성 + PendingInQuantity 업데이트
    /// </summary>
    Task<Order> CompleteOrderAsync(int orderId);

    /// <summary>
    /// 발주 취소
    /// </summary>
    Task CancelOrderAsync(int orderId);
}
