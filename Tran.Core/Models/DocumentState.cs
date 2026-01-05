namespace Tran.Core.Models;

/// <summary>
/// 거래명세표의 상태를 나타내는 열거형
/// "상태가 곧 권한이다" - 모든 UI/권한은 이 상태로 통제됨
/// </summary>
public enum DocumentState
{
    /// <summary>
    /// 작성중 - 송신자가 작성 중인 상태
    /// </summary>
    Draft,

    /// <summary>
    /// 전송됨 - 상대방에게 전달 완료
    /// </summary>
    Sent,

    /// <summary>
    /// 수신됨 - 상대방이 수신 확인
    /// </summary>
    Received,

    /// <summary>
    /// 수정요청 - 수신자가 수정 요청
    /// </summary>
    RevisionRequested,

    /// <summary>
    /// 확정됨 - 거래 조건 최종 확정 (불변)
    /// </summary>
    Confirmed,

    /// <summary>
    /// 구버전 - 새 버전 생성으로 대체됨
    /// </summary>
    Superseded,

    /// <summary>
    /// 취소됨 - 송신자가 전송 전 취소
    /// </summary>
    Cancelled
}
