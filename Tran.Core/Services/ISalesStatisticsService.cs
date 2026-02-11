namespace Tran.Core.Services;

// ═══════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════

/// <summary>
/// 대시보드 요약 카드
/// </summary>
public class SalesSummaryDto
{
    public decimal TodaySales { get; set; }
    public int TodayCount { get; set; }
    public decimal MonthSales { get; set; }
    public int MonthCount { get; set; }
    public decimal PrevMonthSales { get; set; }
    public int DraftCount { get; set; }
    public int LowStockCount { get; set; }
    public decimal MonthPurchases { get; set; }
    public int PendingOrderCount { get; set; }
    public int PendingDeliveryCount { get; set; }
}

/// <summary>
/// 거래처별 매출 집계
/// </summary>
public class CompanySalesRankDto
{
    public string CompanyId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public int SaleCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageAmount { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal Profit => TotalAmount - PurchaseAmount;
    public decimal ProfitRate => TotalAmount > 0 ? Math.Round(Profit / TotalAmount * 100, 1) : 0;
    public double AmountBarRatio { get; set; }
}

/// <summary>
/// 상품별 매출 집계
/// </summary>
public class ProductSalesDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountRatio { get; set; }
    public string AbcRank { get; set; } = "C";
}

/// <summary>
/// 월별 매출/매입 추이
/// </summary>
public class MonthlyTrendDto
{
    public string Label { get; set; } = string.Empty;
    public decimal SalesAmount { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal Profit => SalesAmount - PurchaseAmount;
    public decimal ProfitRate => SalesAmount > 0 ? Math.Round(Profit / SalesAmount * 100, 1) : 0;
    public int SaleCount { get; set; }
    public double SalesBarRatio { get; set; }
    public double PurchaseBarRatio { get; set; }
}

/// <summary>
/// 카테고리별 매출 집계
/// </summary>
public class CategorySalesDto
{
    public string Category { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal AmountRatio { get; set; }
    public double BarRatio { get; set; }
}

/// <summary>
/// 품목별 세부 통계 (마스터-디테일 좌측 목록)
/// </summary>
public class ProductDetailStatsDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string? Category { get; set; }
    public string Unit { get; set; } = "개";

    // 판매 집계
    public decimal TotalSalesAmount { get; set; }
    public decimal TotalSalesQuantity { get; set; }
    public decimal AvgSaleUnitPrice { get; set; }

    // 매입 집계
    public decimal TotalPurchaseAmount { get; set; }
    public decimal TotalPurchaseQuantity { get; set; }
    public decimal AvgPurchaseUnitPrice { get; set; }

    // 마진
    public decimal GrossProfit => TotalSalesAmount - TotalPurchaseAmount;
    public decimal GrossProfitRate => TotalSalesAmount > 0 ? Math.Round(GrossProfit / TotalSalesAmount * 100, 1) : 0;

    // 재고
    public decimal CurrentStock { get; set; }
    public decimal SafetyStock { get; set; }

    // 기타
    public int CustomerCount { get; set; }
    public string AbcRank { get; set; } = "C";
    public string TrendDirection { get; set; } = "→";
}

/// <summary>
/// 품목 월별 매출/매입 내역 (디테일 바차트용)
/// </summary>
public class ProductMonthlyBreakdownDto
{
    public string MonthLabel { get; set; } = string.Empty;
    public decimal SalesQuantity { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal PurchaseQuantity { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal Profit => SalesAmount - PurchaseAmount;
    public double SalesBarRatio { get; set; }
}

/// <summary>
/// 품목 거래처별 분포 (디테일 우측 하단)
/// </summary>
public class ProductCustomerDistributionDto
{
    public string CompanyId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountRatio { get; set; }
    public double BarRatio { get; set; }
}

// ═══════════════════════════════════════════════════════════
// Interface
// ═══════════════════════════════════════════════════════════

/// <summary>
/// 매출 통계 서비스 인터페이스
/// </summary>
public interface ISalesStatisticsService
{
    /// <summary>
    /// 대시보드 요약 데이터 조회
    /// </summary>
    Task<SalesSummaryDto> GetSalesSummaryAsync();

    /// <summary>
    /// 거래처별 매출 집계
    /// </summary>
    Task<List<CompanySalesRankDto>> GetSalesByCompanyAsync(DateTime from, DateTime to);

    /// <summary>
    /// 상품별 매출 집계 (ABC 분석 포함)
    /// </summary>
    Task<List<ProductSalesDto>> GetSalesByProductAsync(DateTime from, DateTime to);

    /// <summary>
    /// 월별 매출/매입 추이
    /// </summary>
    Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int months);

    /// <summary>
    /// 카테고리별 매출 집계
    /// </summary>
    Task<List<CategorySalesDto>> GetSalesByCategoryAsync(DateTime from, DateTime to);

    /// <summary>
    /// 품목별 세부 통계 (마스터-디테일용)
    /// </summary>
    Task<List<ProductDetailStatsDto>> GetProductDetailStatsAsync(DateTime from, DateTime to);

    /// <summary>
    /// 특정 품목의 월별 매출/매입 내역
    /// </summary>
    Task<List<ProductMonthlyBreakdownDto>> GetProductMonthlyBreakdownAsync(int productId, int months);

    /// <summary>
    /// 특정 품목의 거래처별 분포
    /// </summary>
    Task<List<ProductCustomerDistributionDto>> GetProductCustomerDistributionAsync(int productId, DateTime from, DateTime to);
}
