namespace Tran.Core.Models;

public class Purchase
{
    public int PurchaseId { get; set; }
    public int? OrderId { get; set; }  // nullable: can exist without order
    public string CompanyId { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; } = DateTime.Today;
    public PurchaseState State { get; set; } = PurchaseState.PendingDelivery;
    public decimal TotalAmount { get; set; }
    public string? Memo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }

    // Navigation
    public Order? Order { get; set; }
    public Company Company { get; set; } = null!;
    public List<PurchaseItem> Items { get; set; } = new();
}
