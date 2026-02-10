namespace Tran.Core.Models;

public class CompanyProduct
{
    public int CompanyProductId { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public int OrderCount { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Company Company { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
