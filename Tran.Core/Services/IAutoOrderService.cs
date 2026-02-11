using Tran.Core.Models;

namespace Tran.Core.Services;

/// <summary>
/// 발주 제안 행
/// </summary>
public class OrderSuggestionDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal ShortageQty { get; set; }
    public decimal SuggestedQty { get; set; }
    public string? SuggestedCompanyId { get; set; }
    public string? SuggestedCompanyName { get; set; }
    public decimal? SuggestedUnitPrice { get; set; }
}

/// <summary>
/// 안전재고 기반 자동 발주 제안 서비스
/// </summary>
public interface IAutoOrderService
{
    /// <summary>
    /// 안전재고 미달 품목의 발주 제안 목록
    /// </summary>
    Task<List<OrderSuggestionDto>> GetOrderSuggestionsAsync();

    /// <summary>
    /// 제안 기반 발주 Draft 생성
    /// </summary>
    Task<Order> CreateOrderFromSuggestionAsync(int productId, string companyId, decimal qty, decimal unitPrice);
}
