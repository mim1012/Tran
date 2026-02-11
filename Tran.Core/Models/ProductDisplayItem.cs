namespace Tran.Core.Models;

/// <summary>
/// Product + 연관 회사명 표시용 DTO
/// 글로벌 품목관리 화면에서 사용
/// </summary>
public class ProductDisplayItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string? Category { get; set; }
    public string Unit { get; set; } = "개";
    public decimal DefaultPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 연관 회사명 (쉼표 구분)
    /// </summary>
    public string CompanyNames { get; set; } = string.Empty;

    /// <summary>
    /// 연관 회사 ID 목록
    /// </summary>
    public List<string> CompanyIds { get; set; } = new();

    /// <summary>
    /// Product 엔티티로부터 DisplayItem 생성
    /// </summary>
    public static ProductDisplayItem FromProduct(Product product, string companyNames, List<string> companyIds)
    {
        return new ProductDisplayItem
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            ProductCode = product.ProductCode,
            Category = product.Category,
            Unit = product.Unit,
            DefaultPrice = product.DefaultPrice,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            CompanyNames = companyNames,
            CompanyIds = companyIds
        };
    }

    /// <summary>
    /// Product 엔티티로 변환 (편집/저장용)
    /// </summary>
    public Product ToProduct()
    {
        return new Product
        {
            ProductId = ProductId,
            ProductName = ProductName,
            ProductCode = ProductCode,
            Category = Category,
            Unit = Unit,
            DefaultPrice = DefaultPrice,
            IsActive = IsActive,
            CreatedAt = CreatedAt
        };
    }
}
