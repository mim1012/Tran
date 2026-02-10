namespace Tran.Core.Models;

public class Quotation
{
    public int QuotationId { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public DateTime QuotationDate { get; set; } = DateTime.Today;
    public DateTime? ValidUntil { get; set; }
    public QuotationState State { get; set; } = QuotationState.Draft;
    public decimal TotalAmount { get; set; }
    public string? Memo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    // Navigation
    public Company Company { get; set; } = null!;
    public List<QuotationItem> Items { get; set; } = new();
}
