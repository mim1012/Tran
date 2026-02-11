using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Data.Services;

/// <summary>
/// 매출 통계 서비스 구현
/// Sale/Purchase/Inventory 테이블 기반 집계 + ABC 분석
/// </summary>
public class SalesStatisticsService : ISalesStatisticsService
{
    private readonly TranDbContext _context;

    public SalesStatisticsService(TranDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 대시보드 요약 데이터 조회 (매입/발주대기/입고대기 포함)
    /// </summary>
    public async Task<SalesSummaryDto> GetSalesSummaryAsync()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var prevMonthStart = monthStart.AddMonths(-1);
        var prevMonthEnd = monthStart.AddDays(-1);

        var confirmedSales = _context.Sales
            .Where(s => s.State == SaleState.Confirmed);

        var todaySales = await confirmedSales
            .Where(s => s.SaleDate >= today && s.SaleDate < today.AddDays(1))
            .ToListAsync();

        var monthSales = await confirmedSales
            .Where(s => s.SaleDate >= monthStart && s.SaleDate < today.AddDays(1))
            .ToListAsync();

        var prevMonthSalesTotal = await confirmedSales
            .Where(s => s.SaleDate >= prevMonthStart && s.SaleDate <= prevMonthEnd)
            .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

        var draftCount = await _context.Sales
            .CountAsync(s => s.State == SaleState.Draft);

        var lowStockCount = await _context.Inventories
            .CountAsync(inv => inv.SafetyStock > 0 && inv.ConfirmedQuantity - inv.PendingOutQuantity <= inv.SafetyStock);

        var monthPurchases = await _context.Purchases
            .Where(p => p.State == PurchaseState.Delivered
                        && p.PurchaseDate >= monthStart
                        && p.PurchaseDate < today.AddDays(1))
            .SumAsync(p => (decimal?)p.TotalAmount) ?? 0;

        var pendingOrderCount = await _context.Orders
            .CountAsync(o => o.State == OrderState.Draft);

        var pendingDeliveryCount = await _context.Purchases
            .CountAsync(p => p.State == PurchaseState.PendingDelivery || p.State == PurchaseState.PartiallyDelivered);

        return new SalesSummaryDto
        {
            TodaySales = todaySales.Sum(s => s.TotalAmount),
            TodayCount = todaySales.Count,
            MonthSales = monthSales.Sum(s => s.TotalAmount),
            MonthCount = monthSales.Count,
            PrevMonthSales = prevMonthSalesTotal,
            DraftCount = draftCount,
            LowStockCount = lowStockCount,
            MonthPurchases = monthPurchases,
            PendingOrderCount = pendingOrderCount,
            PendingDeliveryCount = pendingDeliveryCount
        };
    }

    /// <summary>
    /// 거래처별 매출 집계 (매입액/이익 포함)
    /// </summary>
    public async Task<List<CompanySalesRankDto>> GetSalesByCompanyAsync(DateTime from, DateTime to)
    {
        var toEnd = to.Date.AddDays(1);

        var salesByCompany = await _context.Sales
            .Include(s => s.Company)
            .Where(s => s.State == SaleState.Confirmed
                        && s.SaleDate >= from.Date
                        && s.SaleDate < toEnd)
            .ToListAsync();

        var grouped = salesByCompany
            .GroupBy(s => new { s.CompanyId, s.Company.CompanyName })
            .Select(g => new
            {
                g.Key.CompanyId,
                g.Key.CompanyName,
                SaleCount = g.Count(),
                TotalAmount = g.Sum(s => s.TotalAmount),
                AverageAmount = g.Average(s => s.TotalAmount)
            })
            .OrderByDescending(r => r.TotalAmount)
            .ToList();

        var purchasesRaw = await _context.Purchases
            .Where(p => p.State == PurchaseState.Delivered
                        && p.PurchaseDate >= from.Date
                        && p.PurchaseDate < toEnd)
            .ToListAsync();

        var purchasesByCompany = purchasesRaw
            .GroupBy(p => p.CompanyId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.TotalAmount));

        var maxAmount = grouped.Count > 0 ? grouped.Max(s => s.TotalAmount) : 0;

        return grouped.Select(s => new CompanySalesRankDto
        {
            CompanyId = s.CompanyId,
            CompanyName = s.CompanyName,
            SaleCount = s.SaleCount,
            TotalAmount = s.TotalAmount,
            AverageAmount = s.AverageAmount,
            PurchaseAmount = purchasesByCompany.GetValueOrDefault(s.CompanyId, 0),
            AmountBarRatio = maxAmount > 0 ? (double)(s.TotalAmount / maxAmount) : 0
        }).ToList();
    }

    /// <summary>
    /// 상품별 매출 집계 (ABC 분석 포함)
    /// </summary>
    public async Task<List<ProductSalesDto>> GetSalesByProductAsync(DateTime from, DateTime to)
    {
        var toEnd = to.Date.AddDays(1);

        var saleItems = await _context.SaleItems
            .Include(i => i.Sale)
            .Include(i => i.Product)
            .Where(i => i.Sale.State == SaleState.Confirmed
                        && i.Sale.SaleDate >= from.Date
                        && i.Sale.SaleDate < toEnd)
            .ToListAsync();

        var items = saleItems
            .GroupBy(i => new { i.ProductId, i.ProductName, i.Product.ProductCode })
            .Select(g => new ProductSalesDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                ProductCode = g.Key.ProductCode,
                TotalQuantity = g.Sum(i => i.Quantity),
                TotalAmount = g.Sum(i => i.LineAmount)
            })
            .OrderByDescending(r => r.TotalAmount)
            .ToList();

        var grandTotal = items.Sum(i => i.TotalAmount);
        if (grandTotal > 0)
        {
            decimal cumulative = 0;
            foreach (var item in items)
            {
                item.AmountRatio = Math.Round(item.TotalAmount / grandTotal * 100, 1);
                cumulative += item.TotalAmount;
                var cumulativeRatio = cumulative / grandTotal;
                item.AbcRank = cumulativeRatio <= 0.7m ? "A"
                             : cumulativeRatio <= 0.9m ? "B"
                             : "C";
            }
        }

        return items;
    }

    /// <summary>
    /// 월별 매출/매입 추이
    /// </summary>
    public async Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int months)
    {
        var today = DateTime.Today;
        var startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-(months - 1));

        var salesRaw = await _context.Sales
            .Where(s => s.State == SaleState.Confirmed && s.SaleDate >= startDate)
            .ToListAsync();

        var purchasesRaw = await _context.Purchases
            .Where(p => p.State == PurchaseState.Delivered && p.PurchaseDate >= startDate)
            .ToListAsync();

        var monthlySales = salesRaw
            .GroupBy(s => $"{s.SaleDate.Year:D4}-{s.SaleDate.Month:D2}")
            .ToDictionary(g => g.Key, g => new { Amount = g.Sum(s => s.TotalAmount), Count = g.Count() });

        var monthlyPurchases = purchasesRaw
            .GroupBy(p => $"{p.PurchaseDate.Year:D4}-{p.PurchaseDate.Month:D2}")
            .ToDictionary(g => g.Key, g => g.Sum(p => p.TotalAmount));

        var result = new List<MonthlyTrendDto>();
        for (int i = 0; i < months; i++)
        {
            var date = startDate.AddMonths(i);
            var label = $"{date.Year:D4}-{date.Month:D2}";

            result.Add(new MonthlyTrendDto
            {
                Label = label,
                SalesAmount = monthlySales.ContainsKey(label) ? monthlySales[label].Amount : 0,
                PurchaseAmount = monthlyPurchases.GetValueOrDefault(label, 0),
                SaleCount = monthlySales.ContainsKey(label) ? monthlySales[label].Count : 0
            });
        }

        var maxSales = result.Max(r => r.SalesAmount);
        var maxPurchase = result.Max(r => r.PurchaseAmount);
        var maxVal = Math.Max(maxSales, maxPurchase);

        if (maxVal > 0)
        {
            foreach (var item in result)
            {
                item.SalesBarRatio = (double)(item.SalesAmount / maxVal);
                item.PurchaseBarRatio = (double)(item.PurchaseAmount / maxVal);
            }
        }

        return result;
    }

    /// <summary>
    /// 카테고리별 매출 집계
    /// </summary>
    public async Task<List<CategorySalesDto>> GetSalesByCategoryAsync(DateTime from, DateTime to)
    {
        var toEnd = to.Date.AddDays(1);

        var saleItems = await _context.SaleItems
            .Include(i => i.Sale)
            .Include(i => i.Product)
            .Where(i => i.Sale.State == SaleState.Confirmed
                        && i.Sale.SaleDate >= from.Date
                        && i.Sale.SaleDate < toEnd)
            .ToListAsync();

        var items = saleItems
            .GroupBy(i => i.Product.Category ?? "미분류")
            .Select(g => new CategorySalesDto
            {
                Category = g.Key,
                ProductCount = g.Select(i => i.ProductId).Distinct().Count(),
                TotalAmount = g.Sum(i => i.LineAmount),
                TotalQuantity = g.Sum(i => i.Quantity)
            })
            .OrderByDescending(r => r.TotalAmount)
            .ToList();

        var grandTotal = items.Sum(i => i.TotalAmount);
        var maxAmount = items.Count > 0 ? items.Max(i => i.TotalAmount) : 0;

        foreach (var item in items)
        {
            item.AmountRatio = grandTotal > 0 ? Math.Round(item.TotalAmount / grandTotal * 100, 1) : 0;
            item.BarRatio = maxAmount > 0 ? (double)(item.TotalAmount / maxAmount) : 0;
        }

        return items;
    }

    /// <summary>
    /// 품목별 세부 통계 (마스터-디테일 목록)
    /// </summary>
    public async Task<List<ProductDetailStatsDto>> GetProductDetailStatsAsync(DateTime from, DateTime to)
    {
        var toEnd = to.Date.AddDays(1);
        var today = DateTime.Today;
        var threeMonthsAgo = today.AddMonths(-3);
        var sixMonthsAgo = today.AddMonths(-6);

        // 판매 항목 로드 (클라이언트 평가)
        var saleItemsRaw = await _context.SaleItems
            .Include(i => i.Sale)
            .Include(i => i.Product)
            .Where(i => i.Sale.State == SaleState.Confirmed
                        && i.Sale.SaleDate >= from.Date
                        && i.Sale.SaleDate < toEnd)
            .ToListAsync();

        var salesByProduct = saleItemsRaw
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => new
            {
                TotalAmount = g.Sum(i => i.LineAmount),
                TotalQuantity = g.Sum(i => i.Quantity),
                AvgUnitPrice = g.Average(i => i.UnitPrice),
                CustomerCount = g.Select(i => i.Sale.CompanyId).Distinct().Count()
            });

        // 매입 항목 로드
        var purchaseItemsRaw = await _context.PurchaseItems
            .Include(i => i.Purchase)
            .Where(i => i.Purchase.State == PurchaseState.Delivered
                        && i.Purchase.PurchaseDate >= from.Date
                        && i.Purchase.PurchaseDate < toEnd)
            .ToListAsync();

        var purchasesByProduct = purchaseItemsRaw
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => new
            {
                TotalAmount = g.Sum(i => i.LineAmount),
                TotalQuantity = g.Sum(i => i.ReceivedQuantity),
                AvgUnitPrice = g.Average(i => i.UnitPrice)
            });

        // 재고 정보
        var inventories = await _context.Inventories
            .ToDictionaryAsync(inv => inv.ProductId, inv => new { inv.ConfirmedQuantity, inv.SafetyStock });

        // 트렌드 계산: 최근 3개월 vs 이전 3개월 매출
        var trendItemsRaw = await _context.SaleItems
            .Include(i => i.Sale)
            .Where(i => i.Sale.State == SaleState.Confirmed && i.Sale.SaleDate >= sixMonthsAgo)
            .ToListAsync();

        var recentSales = trendItemsRaw
            .Where(i => i.Sale.SaleDate >= threeMonthsAgo)
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.LineAmount));

        var olderSales = trendItemsRaw
            .Where(i => i.Sale.SaleDate < threeMonthsAgo)
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.LineAmount));

        // 관련 Product 정보
        var productIds = salesByProduct.Keys.Union(purchasesByProduct.Keys).ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.ProductId))
            .ToListAsync();

        var result = products.Select(p =>
        {
            salesByProduct.TryGetValue(p.ProductId, out var sales);
            purchasesByProduct.TryGetValue(p.ProductId, out var purchases);
            inventories.TryGetValue(p.ProductId, out var inv);
            var recent = recentSales.GetValueOrDefault(p.ProductId, 0);
            var older = olderSales.GetValueOrDefault(p.ProductId, 0);

            string trend;
            if (older == 0 && recent == 0) trend = "→";
            else if (older == 0) trend = "↑";
            else
            {
                var changeRate = (recent - older) / older;
                trend = changeRate > 0.1m ? "↑" : changeRate < -0.1m ? "↓" : "→";
            }

            return new ProductDetailStatsDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                ProductCode = p.ProductCode,
                Category = p.Category,
                Unit = p.Unit,
                TotalSalesAmount = sales?.TotalAmount ?? 0,
                TotalSalesQuantity = sales?.TotalQuantity ?? 0,
                AvgSaleUnitPrice = sales != null ? Math.Round(sales.AvgUnitPrice, 0) : 0,
                TotalPurchaseAmount = purchases?.TotalAmount ?? 0,
                TotalPurchaseQuantity = purchases?.TotalQuantity ?? 0,
                AvgPurchaseUnitPrice = purchases != null ? Math.Round(purchases.AvgUnitPrice, 0) : 0,
                CurrentStock = inv?.ConfirmedQuantity ?? 0,
                SafetyStock = inv?.SafetyStock ?? 0,
                CustomerCount = sales?.CustomerCount ?? 0,
                TrendDirection = trend
            };
        })
        .OrderByDescending(x => x.TotalSalesAmount)
        .ToList();

        // ABC 분석
        var grandTotal = result.Sum(r => r.TotalSalesAmount);
        if (grandTotal > 0)
        {
            decimal cumulative = 0;
            foreach (var item in result)
            {
                cumulative += item.TotalSalesAmount;
                var cumulativeRatio = cumulative / grandTotal;
                item.AbcRank = cumulativeRatio <= 0.7m ? "A"
                             : cumulativeRatio <= 0.9m ? "B"
                             : "C";
            }
        }

        return result;
    }

    /// <summary>
    /// 특정 품목의 월별 매출/매입 내역
    /// </summary>
    public async Task<List<ProductMonthlyBreakdownDto>> GetProductMonthlyBreakdownAsync(int productId, int months)
    {
        var today = DateTime.Today;
        var startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-(months - 1));

        var saleItemsRaw = await _context.SaleItems
            .Include(i => i.Sale)
            .Where(i => i.ProductId == productId
                        && i.Sale.State == SaleState.Confirmed
                        && i.Sale.SaleDate >= startDate)
            .ToListAsync();

        var purchaseItemsRaw = await _context.PurchaseItems
            .Include(i => i.Purchase)
            .Where(i => i.ProductId == productId
                        && i.Purchase.State == PurchaseState.Delivered
                        && i.Purchase.PurchaseDate >= startDate)
            .ToListAsync();

        var monthlySales = saleItemsRaw
            .GroupBy(i => $"{i.Sale.SaleDate.Year:D4}-{i.Sale.SaleDate.Month:D2}")
            .ToDictionary(g => g.Key, g => new { Quantity = g.Sum(i => i.Quantity), Amount = g.Sum(i => i.LineAmount) });

        var monthlyPurchases = purchaseItemsRaw
            .GroupBy(i => $"{i.Purchase.PurchaseDate.Year:D4}-{i.Purchase.PurchaseDate.Month:D2}")
            .ToDictionary(g => g.Key, g => new { Quantity = g.Sum(i => i.ReceivedQuantity), Amount = g.Sum(i => i.LineAmount) });

        var result = new List<ProductMonthlyBreakdownDto>();
        for (int i = 0; i < months; i++)
        {
            var date = startDate.AddMonths(i);
            var label = $"{date.Year:D4}-{date.Month:D2}";

            result.Add(new ProductMonthlyBreakdownDto
            {
                MonthLabel = label,
                SalesQuantity = monthlySales.ContainsKey(label) ? monthlySales[label].Quantity : 0,
                SalesAmount = monthlySales.ContainsKey(label) ? monthlySales[label].Amount : 0,
                PurchaseQuantity = monthlyPurchases.ContainsKey(label) ? monthlyPurchases[label].Quantity : 0,
                PurchaseAmount = monthlyPurchases.ContainsKey(label) ? monthlyPurchases[label].Amount : 0
            });
        }

        var maxSales = result.Count > 0 ? result.Max(r => r.SalesAmount) : 0;
        if (maxSales > 0)
        {
            foreach (var item in result)
                item.SalesBarRatio = (double)(item.SalesAmount / maxSales);
        }

        return result;
    }

    /// <summary>
    /// 특정 품목의 거래처별 분포
    /// </summary>
    public async Task<List<ProductCustomerDistributionDto>> GetProductCustomerDistributionAsync(int productId, DateTime from, DateTime to)
    {
        var toEnd = to.Date.AddDays(1);

        var saleItemsRaw = await _context.SaleItems
            .Include(i => i.Sale)
                .ThenInclude(s => s.Company)
            .Where(i => i.ProductId == productId
                        && i.Sale.State == SaleState.Confirmed
                        && i.Sale.SaleDate >= from.Date
                        && i.Sale.SaleDate < toEnd)
            .ToListAsync();

        var items = saleItemsRaw
            .GroupBy(i => new { i.Sale.CompanyId, i.Sale.Company.CompanyName })
            .Select(g => new ProductCustomerDistributionDto
            {
                CompanyId = g.Key.CompanyId,
                CompanyName = g.Key.CompanyName,
                Quantity = g.Sum(x => x.Quantity),
                Amount = g.Sum(x => x.LineAmount)
            })
            .OrderByDescending(r => r.Amount)
            .ToList();

        var grandTotal = items.Sum(i => i.Amount);
        var maxAmount = items.Count > 0 ? items.Max(i => i.Amount) : 0;

        foreach (var item in items)
        {
            item.AmountRatio = grandTotal > 0 ? Math.Round(item.Amount / grandTotal * 100, 1) : 0;
            item.BarRatio = maxAmount > 0 ? (double)(item.Amount / maxAmount) : 0;
        }

        return items;
    }
}
