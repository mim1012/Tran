using Tran.Core.Models;

namespace Tran.Core.Services;

/// <summary>
/// 상태 전이 서비스 인터페이스
/// "상태가 곧 권한이다" - 모든 상태 전이를 통제
/// </summary>
public interface IStateTransitionService
{
    /// <summary>
    /// 상태 전이 가능 여부 검증
    /// </summary>
    /// <param name="from">현재 상태</param>
    /// <param name="to">목표 상태</param>
    /// <returns>전이 가능 여부</returns>
    bool CanTransition(DocumentState from, DocumentState to);

    /// <summary>
    /// 상태 전이 실행
    /// </summary>
    /// <param name="document">문서</param>
    /// <param name="toState">목표 상태</param>
    /// <param name="changedBy">변경자</param>
    /// <param name="reason">변경 사유 (선택)</param>
    /// <returns>전이 성공 여부</returns>
    Task<StateTransitionResult> TransitionAsync(
        Document document,
        DocumentState toState,
        string changedBy,
        string? reason = null);

    /// <summary>
    /// 상태별 허용되는 다음 상태 목록
    /// </summary>
    /// <param name="state">현재 상태</param>
    /// <returns>허용되는 다음 상태들</returns>
    IEnumerable<DocumentState> GetAllowedTransitions(DocumentState state);
}

/// <summary>
/// 상태 전이 결과
/// </summary>
public class StateTransitionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DocumentStateLog? StateLog { get; set; }
}
