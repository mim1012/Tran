using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Data.Services;

/// <summary>
/// 단가 정책 서비스 구현
/// 거래처별 단가 관리, 가격 이력, 거래처-품목 연결 관리
/// </summary>
public class PricePolicyService : IPricePolicyService
{
    private readonly TranDbContext _context;

    public PricePolicyService(TranDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 거래처-품목 단가 조회 (null이면 미등록)
    /// </summary>
    public async Task<decimal?> GetCompanyPriceAsync(string companyId, int productId)
    {
        var companyPrice = await _context.CompanyPrices
            .FirstOrDefaultAsync(cp =>
                cp.CompanyId == companyId &&
                cp.ProductId == productId);

        return companyPrice?.UnitPrice;
    }

    /// <summary>
    /// 거래처-품목 단가 등록/변경
    /// 기존 단가가 있으면 PriceHistory 생성 후 업데이트
    /// 없으면 신규 등록
    /// </summary>
    public async Task UpdateCompanyPriceAsync(string companyId, int productId, decimal newPrice, string? reason = null)
    {
        if (newPrice < 0) throw new ArgumentException("단가는 0 이상이어야 합니다.", nameof(newPrice));

        var companyPrice = await _context.CompanyPrices
            .FirstOrDefaultAsync(cp =>
                cp.CompanyId == companyId &&
                cp.ProductId == productId);

        decimal oldPrice = 0;

        if (companyPrice == null)
        {
            // 신규 등록
            companyPrice = new CompanyPrice
            {
                CompanyId = companyId,
                ProductId = productId,
                UnitPrice = newPrice,
                EffectiveFrom = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.CompanyPrices.Add(companyPrice);
        }
        else
        {
            // 기존 단가 업데이트
            oldPrice = companyPrice.UnitPrice;
            companyPrice.UnitPrice = newPrice;
            companyPrice.EffectiveFrom = DateTime.UtcNow;
        }

        // 가격 변경 이력 생성
        var priceHistory = new PriceHistory
        {
            CompanyId = companyId,
            ProductId = productId,
            OldPrice = oldPrice,
            NewPrice = newPrice,
            ChangedAt = DateTime.UtcNow,
            Reason = reason
        };
        _context.PriceHistories.Add(priceHistory);

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 특정 거래처의 전체 단가 목록
    /// </summary>
    public async Task<List<CompanyPrice>> GetCompanyPricesAsync(string companyId)
    {
        return await _context.CompanyPrices
            .Include(cp => cp.Product)
            .Where(cp => cp.CompanyId == companyId)
            .OrderBy(cp => cp.Product.ProductName)
            .ToListAsync();
    }

    /// <summary>
    /// 특정 거래처-품목의 단가 변경 이력
    /// </summary>
    public async Task<List<PriceHistory>> GetPriceHistoryAsync(string companyId, int productId)
    {
        return await _context.PriceHistories
            .Where(ph =>
                ph.CompanyId == companyId &&
                ph.ProductId == productId)
            .OrderByDescending(ph => ph.ChangedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 특정 거래처의 전체 단가 변경 이력 (Product/Company 네비게이션 포함)
    /// </summary>
    public async Task<List<PriceHistory>> GetAllPriceHistoriesForCompanyAsync(string companyId)
    {
        return await _context.PriceHistories
            .Include(ph => ph.Product)
            .Include(ph => ph.Company)
            .Where(ph => ph.CompanyId == companyId)
            .OrderByDescending(ph => ph.ChangedAt)
            .Take(100)
            .ToListAsync();
    }

    /// <summary>
    /// 거래처-품목 연결 등록
    /// 이미 등록된 경우 무시
    /// </summary>
    public async Task RegisterCompanyProductAsync(string companyId, int productId)
    {
        var existing = await _context.CompanyProducts
            .FirstOrDefaultAsync(cp =>
                cp.CompanyId == companyId &&
                cp.ProductId == productId);

        if (existing != null)
        {
            return; // 이미 등록됨
        }

        var companyProduct = new CompanyProduct
        {
            CompanyId = companyId,
            ProductId = productId,
            OrderCount = 0,
            LastOrderDate = null,
            CreatedAt = DateTime.UtcNow
        };
        _context.CompanyProducts.Add(companyProduct);

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 거래처-품목 연결 제거
    /// </summary>
    public async Task RemoveCompanyProductAsync(string companyId, int productId)
    {
        var companyProduct = await _context.CompanyProducts
            .FirstOrDefaultAsync(cp => cp.CompanyId == companyId && cp.ProductId == productId);
        if (companyProduct != null)
        {
            _context.CompanyProducts.Remove(companyProduct);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 특정 거래처의 거래 품목 목록
    /// </summary>
    public async Task<List<CompanyProduct>> GetCompanyProductsAsync(string companyId)
    {
        return await _context.CompanyProducts
            .Include(cp => cp.Product)
            .Where(cp => cp.CompanyId == companyId)
            .OrderByDescending(cp => cp.OrderCount)
            .ToListAsync();
    }
}
