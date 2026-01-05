namespace Tran.Core.Models;

/// <summary>
/// 상태 로그 - 가장 중요한 테이블
/// 분쟁 시 최종 증빙
/// 삭제/수정 불가
/// 서버로 보내도 되는 유일한 "신뢰 데이터"
/// </summary>
public class DocumentStateLog
{
    public string LogId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public DocumentState FromState { get; set; }
    public DocumentState ToState { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 변경 사유 (특히 수정 요청 시)
    /// </summary>
    public string? Reason { get; set; }
}
