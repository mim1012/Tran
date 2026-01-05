namespace Tran.Core.Models;

/// <summary>
/// 수정 요청 엔티티
/// </summary>
public class RevisionRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string RequestReason { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public RevisionRequestStatus Status { get; set; } = RevisionRequestStatus.Open;
}

public enum RevisionRequestStatus
{
    Open,
    Resolved
}
