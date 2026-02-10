namespace Tran.Core.Models;

public class PurchaseItem
{
    public int PurchaseItemId { get; set; }
    public int PurchaseId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal DefectQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public string? Note { get; set; }

    // Navigation
    public Purchase Purchase { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
