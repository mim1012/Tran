namespace Tran.Core.Models;

public class QuotationItem
{
    public int QuotationItemId { get; set; }
    public int QuotationId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public string? Note { get; set; }

    // Navigation
    public Quotation Quotation { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
