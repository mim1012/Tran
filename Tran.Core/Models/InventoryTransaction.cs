namespace Tran.Core.Models;

public class InventoryTransaction
{
    public int TransactionId { get; set; }
    public int ProductId { get; set; }
    public InventoryTransactionType Type { get; set; }
    public decimal Quantity { get; set; }
    public string? ReferenceType { get; set; }  // "Sale", "Purchase", "Adjustment"
    public int? ReferenceId { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Product Product { get; set; } = null!;
}
