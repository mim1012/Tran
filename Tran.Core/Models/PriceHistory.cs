namespace Tran.Core.Models;

public class PriceHistory
{
    public int PriceHistoryId { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;
    public Company Company { get; set; } = null!;
}
