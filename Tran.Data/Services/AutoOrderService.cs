using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Data.Services;

/// <summary>
/// 안전재고 기반 자동 발주 제안 서비스 구현
/// </summary>
public class AutoOrderService : IAutoOrderService
{
    private readonly TranDbContext _context;

    public AutoOrderService(TranDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 안전재고 미달 품목의 발주 제안 목록
    /// Inventory에서 AvailableQuantity <= SafetyStock인 품목 필터
    /// CompanyProduct에서 OrderCount 최대 거래처 추천
    /// CompanyPrice에서 단가 조회
    /// </summary>
    public async Task<List<OrderSuggestionDto>> GetOrderSuggestionsAsync()
    {
        // 안전재고 미달 품목 조회
        var lowStockItems = await _context.Inventories
            .Include(inv => inv.Product)
            .Where(inv => inv.SafetyStock > 0
                          && (inv.ConfirmedQuantity - inv.PendingOutQuantity) <= inv.SafetyStock)
            .ToListAsync();

        var suggestions = new List<OrderSuggestionDto>();

        foreach (var inv in lowStockItems)
        {
            var available = inv.ConfirmedQuantity - inv.PendingOutQuantity;
            var suggestedQty = inv.SafetyStock * 2 - available;
            if (suggestedQty <= 0) suggestedQty = inv.SafetyStock;

            // 추천 거래처: OrderCount 최대인 거래처
            var bestCompanyProduct = await _context.CompanyProducts
                .Include(cp => cp.Company)
                .Where(cp => cp.ProductId == inv.ProductId)
                .OrderByDescending(cp => cp.OrderCount)
                .FirstOrDefaultAsync();

            // 추천 단가: 해당 거래처의 CompanyPrice
            decimal? suggestedPrice = null;
            if (bestCompanyProduct != null)
            {
                suggestedPrice = await _context.CompanyPrices
                    .Where(cp => cp.CompanyId == bestCompanyProduct.CompanyId
                                 && cp.ProductId == inv.ProductId)
                    .Select(cp => (decimal?)cp.UnitPrice)
                    .FirstOrDefaultAsync();
            }

            suggestions.Add(new OrderSuggestionDto
            {
                ProductId = inv.ProductId,
                ProductName = inv.Product?.ProductName ?? $"품목 #{inv.ProductId}",
                ProductCode = inv.Product?.ProductCode,
                CurrentStock = available,
                SafetyStock = inv.SafetyStock,
                ShortageQty = inv.SafetyStock - available,
                SuggestedQty = suggestedQty,
                SuggestedCompanyId = bestCompanyProduct?.CompanyId,
                SuggestedCompanyName = bestCompanyProduct?.Company?.CompanyName,
                SuggestedUnitPrice = suggestedPrice
            });
        }

        return suggestions.OrderByDescending(s => s.ShortageQty).ToList();
    }

    /// <summary>
    /// 제안 기반 발주 Draft 생성
    /// </summary>
    public async Task<Order> CreateOrderFromSuggestionAsync(int productId, string companyId, decimal qty, decimal unitPrice)
    {
        // 품목명 조회
        var product = await _context.Products.FindAsync(productId);
        var productName = product?.ProductName ?? $"품목 #{productId}";

        var order = new Order
        {
            CompanyId = companyId,
            OrderDate = DateTime.UtcNow,
            State = OrderState.Draft,
            Memo = "안전재고 기반 자동 발주 제안",
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = productId,
                    ProductName = productName,
                    Quantity = qty,
                    UnitPrice = unitPrice,
                    LineAmount = qty * unitPrice
                }
            }
        };

        order.TotalAmount = order.Items.Sum(i => i.LineAmount);

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return order;
    }
}
