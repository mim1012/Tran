namespace Tran.Core.Models;

public class Inventory
{
    public int InventoryId { get; set; }
    public int ProductId { get; set; }  // Unique constraint
    public decimal ConfirmedQuantity { get; set; }
    public decimal PendingInQuantity { get; set; }
    public decimal PendingOutQuantity { get; set; }
    public decimal SafetyStock { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed
    public decimal AvailableQuantity => ConfirmedQuantity - PendingOutQuantity;

    // Navigation
    public Product Product { get; set; } = null!;
}
