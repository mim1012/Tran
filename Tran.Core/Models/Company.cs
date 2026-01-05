namespace Tran.Core.Models;

/// <summary>
/// 거래처 / 회사 엔티티
/// 문서의 from_company_id, to_company_id 기준이 됨
/// </summary>
public class Company
{
    public string CompanyId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public CompanyStatus Status { get; set; } = CompanyStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum CompanyStatus
{
    Active,
    Inactive
}
