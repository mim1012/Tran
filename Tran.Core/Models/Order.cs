namespace Tran.Core.Models;

public class Order
{
    public int OrderId { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.Today;
    public OrderState State { get; set; } = OrderState.Draft;
    public decimal TotalAmount { get; set; }
    public string? Memo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public Company Company { get; set; } = null!;
    public List<OrderItem> Items { get; set; } = new();
}
