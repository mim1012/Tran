namespace Tran.Core.Models;

/// <summary>
/// 규격 항목 (Key-Value)
/// 품목의 하위 구조로, 컬럼이 아닌 메타데이터로 취급
/// </summary>
public class SpecEntry
{
    /// <summary>
    /// 규격명 (예: "두께", "사이즈", "재질")
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 규격 값 (예: "1.2T", "4 x 8", "SS400")
    /// 항상 문자열로 저장 (의미 해석 안 함)
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
