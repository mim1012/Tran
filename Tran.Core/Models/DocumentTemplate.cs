namespace Tran.Core.Models;

/// <summary>
/// 문서 양식 (회사별 커스터마이징)
/// "양식 = 렌더링/입력 규칙"으로 분리
/// DB 구조는 변경하지 않고 표현 방식만 커스터마이징
/// </summary>
public class DocumentTemplate
{
    /// <summary>
    /// 양식 ID (PK)
    /// </summary>
    public string TemplateId { get; set; } = null!;

    /// <summary>
    /// 회사 ID (FK)
    /// </summary>
    public string CompanyId { get; set; } = null!;

    /// <summary>
    /// 양식 이름 (예: "A사 표준 거래명세표")
    /// </summary>
    public string TemplateName { get; set; } = null!;

    /// <summary>
    /// 양식 타입 (거래명세표 / 정산서)
    /// </summary>
    public TemplateType TemplateType { get; set; }

    /// <summary>
    /// 필드 정의 (JSON)
    /// 예: {"fields": [{"key": "item_name", "label": "상품명", "type": "text", "required": true}]}
    /// </summary>
    public string SchemaJson { get; set; } = null!;

    /// <summary>
    /// UI/PDF 레이아웃 정의 (JSON)
    /// </summary>
    public string LayoutJson { get; set; } = null!;

    /// <summary>
    /// 생성 일시
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 활성화 여부 (기본 양식으로 사용할지)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 회사 참조
    /// </summary>
    public Company? Company { get; set; }
}
