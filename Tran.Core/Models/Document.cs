namespace Tran.Core.Models;

/// <summary>
/// 문서 마스터 - 상태의 중심
/// "문서 테이블이 아니라, 상태 테이블을 중심으로 설계한다"
/// </summary>
public class Document
{
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>
    /// 이전 버전 (nullable) - 버전 트리 구성
    /// </summary>
    public string? ParentDocumentId { get; set; }

    /// <summary>
    /// 버전 번호 - 사용자 인지용
    /// </summary>
    public int VersionNumber { get; set; } = 1;

    public string FromCompanyId { get; set; } = string.Empty;
    public string ToCompanyId { get; set; } = string.Empty;

    /// <summary>
    /// 현재 상태 - 모든 UI/권한의 기준
    /// </summary>
    public DocumentState State { get; set; } = DocumentState.Draft;

    /// <summary>
    /// 상태 버전 - Optimistic Locking용
    /// </summary>
    public int StateVersion { get; set; } = 0;

    public decimal TotalAmount { get; set; }

    /// <summary>
    /// 문서 해시 (SHA-256) - 위변조 검증
    /// </summary>
    public string? ContentHash { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// 거래일
    /// </summary>
    public DateTime TransactionDate { get; set; } = DateTime.Today;

    /// <summary>
    /// 비고 (상대방 공개)
    /// </summary>
    public string? Memo { get; set; }

    /// <summary>
    /// 내부 메모 (상대방 비공개)
    /// </summary>
    public string? InternalMemo { get; set; }
}
