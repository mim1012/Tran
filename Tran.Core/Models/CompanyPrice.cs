namespace Tran.Core.Models;

public class CompanyPrice
{
    public int CompanyPriceId { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Company Company { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
