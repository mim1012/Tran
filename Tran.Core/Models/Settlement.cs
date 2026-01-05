namespace Tran.Core.Models;

/// <summary>
/// 정산 엔티티
/// documents.state = CONFIRMED만 집계 대상
/// 문서 삭제되어도 정산 기록은 유지
/// </summary>
public class Settlement
{
    public string SettlementId { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? SettledAt { get; set; }
}
