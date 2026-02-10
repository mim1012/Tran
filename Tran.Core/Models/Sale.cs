namespace Tran.Core.Models;

public class Sale
{
    public int SaleId { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; } = DateTime.Today;
    public SaleState State { get; set; } = SaleState.Draft;
    public decimal TotalAmount { get; set; }
    public string? Memo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }

    // Navigation
    public Company Company { get; set; } = null!;
    public List<SaleItem> Items { get; set; } = new();
}
