using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Data.Services;

/// <summary>
/// 품목 관리 서비스 구현
/// EF Core를 사용한 품목 CRUD
/// </summary>
public class ProductService : IProductService
{
    private readonly TranDbContext _context;

    public ProductService(TranDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 전체 품목 목록 조회
    /// </summary>
    public async Task<List<Product>> GetAllProductsAsync(bool includeInactive = false)
    {
        var query = _context.Products.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .OrderBy(p => p.ProductName)
            .ToListAsync();
    }

    /// <summary>
    /// 품목명/품목코드 키워드 검색
    /// </summary>
    public async Task<List<Product>> SearchProductsAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return await GetAllProductsAsync();
        }

        var normalizedKeyword = keyword.Trim().ToLower();

        return await _context.Products
            .Where(p => p.IsActive)
            .Where(p =>
                p.ProductName.ToLower().Contains(normalizedKeyword) ||
                (p.ProductCode != null && p.ProductCode.ToLower().Contains(normalizedKeyword)) ||
                (p.Category != null && p.Category.ToLower().Contains(normalizedKeyword)))
            .OrderBy(p => p.ProductName)
            .ToListAsync();
    }

    /// <summary>
    /// 품목 ID로 조회
    /// </summary>
    public async Task<Product?> GetByIdAsync(int productId)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.ProductId == productId);
    }

    /// <summary>
    /// 새 품목 생성
    /// </summary>
    public async Task<Product> CreateAsync(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (string.IsNullOrWhiteSpace(product.ProductName))
            throw new ArgumentException("품목명은 필수입니다.", nameof(product));

        product.CreatedAt = DateTime.UtcNow;
        product.IsActive = true;

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product;
    }

    /// <summary>
    /// 품목 정보 수정
    /// </summary>
    public async Task UpdateAsync(Product product)
    {
        var existing = await _context.Products
            .FirstOrDefaultAsync(p => p.ProductId == product.ProductId);

        if (existing == null)
        {
            throw new InvalidOperationException($"품목을 찾을 수 없습니다. (ID: {product.ProductId})");
        }

        if (string.IsNullOrWhiteSpace(product.ProductName))
            throw new ArgumentException("품목명은 필수입니다.", nameof(product));

        existing.ProductName = product.ProductName;
        existing.ProductCode = product.ProductCode;
        existing.Category = product.Category;
        existing.Unit = product.Unit;
        existing.DefaultPrice = product.DefaultPrice;
        existing.IsActive = product.IsActive;

        await _context.SaveChangesAsync();
    }
}
