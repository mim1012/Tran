using Tran.Core.Models;

namespace Tran.Core.Services;

/// <summary>
/// 상태 전이 서비스 구현
/// PRD의 State Machine Definition 규칙을 엄격히 준수
/// </summary>
public class StateTransitionService : IStateTransitionService
{
    // 상태 전이 규칙 (PRD Step 3의 상태 전이 테이블)
    private static readonly Dictionary<DocumentState, HashSet<DocumentState>> _allowedTransitions = new()
    {
        [DocumentState.Draft] = new()
        {
            DocumentState.Sent,
            DocumentState.Cancelled
        },
        [DocumentState.Sent] = new()
        {
            DocumentState.Received
        },
        [DocumentState.Received] = new()
        {
            DocumentState.Confirmed,
            DocumentState.RevisionRequested
        },
        [DocumentState.RevisionRequested] = new()
        {
            DocumentState.Draft  // 새 버전 생성
        },
        [DocumentState.Confirmed] = new HashSet<DocumentState>(), // 종결 상태
        [DocumentState.Superseded] = new HashSet<DocumentState>(), // 종결 상태
        [DocumentState.Cancelled] = new HashSet<DocumentState>()  // 종결 상태
    };

    public bool CanTransition(DocumentState from, DocumentState to)
    {
        if (!_allowedTransitions.ContainsKey(from))
            return false;

        return _allowedTransitions[from].Contains(to);
    }

    public async Task<StateTransitionResult> TransitionAsync(
        Document document,
        DocumentState toState,
        string changedBy,
        string? reason = null)
    {
        // 1. 전이 가능 여부 검증
        if (!CanTransition(document.State, toState))
        {
            return new StateTransitionResult
            {
                Success = false,
                ErrorMessage = $"상태 전이 불가: {document.State} → {toState}"
            };
        }

        // 2. 이전 상태 저장
        var fromState = document.State;

        // 3. 상태 전이 실행
        document.State = toState;
        document.StateVersion++; // Optimistic Locking

        // 4. 타임스탬프 업데이트
        if (toState == DocumentState.Sent)
        {
            document.SentAt = DateTime.UtcNow;
        }
        else if (toState == DocumentState.Confirmed)
        {
            document.ConfirmedAt = DateTime.UtcNow;
        }

        // 5. 상태 로그 생성 (분쟁 시 증빙)
        var stateLog = new DocumentStateLog
        {
            LogId = Guid.NewGuid().ToString(),
            DocumentId = document.DocumentId,
            FromState = fromState,
            ToState = toState,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow,
            Reason = reason
        };

        return await Task.FromResult(new StateTransitionResult
        {
            Success = true,
            StateLog = stateLog
        });
    }

    public IEnumerable<DocumentState> GetAllowedTransitions(DocumentState state)
    {
        if (!_allowedTransitions.ContainsKey(state))
            return Enumerable.Empty<DocumentState>();

        return _allowedTransitions[state];
    }
}
