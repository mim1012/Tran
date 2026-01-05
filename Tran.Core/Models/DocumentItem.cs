namespace Tran.Core.Models;

/// <summary>
/// 거래명세표 품목
/// CONFIRMED 상태에서 INSERT/UPDATE 금지
/// </summary>
public class DocumentItem
{
    public string ItemId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// 수량 - 소수점 2자리 고정
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 단가 - 소수점 2자리 고정
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// 옵션/비고 (예: 색상, 사이즈 등)
    /// </summary>
    public string? OptionText { get; set; }

    /// <summary>
    /// 라인 금액 (자동 계산: Quantity * UnitPrice)
    /// </summary>
    public decimal LineAmount { get; set; }

    /// <summary>
    /// 양식별 확장 필드 (JSON)
    /// 예: {"origin": "대한민국", "spec": "128GB"}
    /// DB 컬럼 변경 없이 회사별 추가 필드 지원
    /// </summary>
    public string? ExtraDataJson { get; set; }

    /// <summary>
    /// 라인 금액 계산
    /// </summary>
    public void CalculateLineAmount()
    {
        LineAmount = Quantity * UnitPrice;
    }
}
