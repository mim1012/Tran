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
        // 0. UserContext 초기화 여부 검증
        if (!UserContext.IsInitialized)
        {
            return new StateTransitionResult
            {
                Success = false,
                ErrorMessage = "사용자 정보가 설정되지 않았습니다. 거래처를 먼저 선택해주세요."
            };
        }

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

        // 3. RevisionRequested→Draft 특수 처리: 새 버전 생성
        Document? newVersionDocument = null;
        if (fromState == DocumentState.RevisionRequested && toState == DocumentState.Draft)
        {
            // 원본 문서를 Superseded로 변경
            document.State = DocumentState.Superseded;
            document.StateVersion++;

            // 새 버전 문서 생성 - 기존 -V 접미사를 strip하여 중첩 방지
            var baseDocumentId = document.DocumentId;
            var vSuffixIndex = baseDocumentId.LastIndexOf("-V", StringComparison.Ordinal);
            if (vSuffixIndex >= 0)
            {
                baseDocumentId = baseDocumentId[..vSuffixIndex];
            }

            newVersionDocument = new Document
            {
                DocumentId = $"{baseDocumentId}-V{document.VersionNumber + 1}",
                ParentDocumentId = document.DocumentId,
                VersionNumber = document.VersionNumber + 1,
                FromCompanyId = document.FromCompanyId,
                ToCompanyId = document.ToCompanyId,
                State = DocumentState.Draft,
                StateVersion = 0,
                TotalAmount = document.TotalAmount,
                ContentHash = document.ContentHash,
                CreatedBy = changedBy,
                CreatedAt = DateTime.UtcNow,
                TransactionDate = document.TransactionDate,
                Memo = document.Memo,
                InternalMemo = document.InternalMemo
            };

            // 원본 Superseded 로그
            var supersededLog = new DocumentStateLog
            {
                LogId = Guid.NewGuid().ToString(),
                DocumentId = document.DocumentId,
                FromState = fromState,
                ToState = DocumentState.Superseded,
                ChangedBy = changedBy,
                ChangedAt = DateTime.UtcNow,
                Reason = reason ?? "새 버전 생성으로 인한 구버전 처리"
            };

            // 새 버전 Draft 생성 로그
            var newDraftLog = new DocumentStateLog
            {
                LogId = Guid.NewGuid().ToString(),
                DocumentId = newVersionDocument.DocumentId,
                FromState = DocumentState.RevisionRequested,
                ToState = DocumentState.Draft,
                ChangedBy = changedBy,
                ChangedAt = DateTime.UtcNow,
                Reason = reason ?? "수정 요청에 의한 새 버전 생성"
            };

            return await Task.FromResult(new StateTransitionResult
            {
                Success = true,
                StateLogs = new List<DocumentStateLog> { supersededLog, newDraftLog },
                NewVersionDocument = newVersionDocument
            });
        }

        // 4. 일반 상태 전이 실행
        document.State = toState;
        document.StateVersion++; // Optimistic Locking

        // 5. 타임스탬프 업데이트
        if (toState == DocumentState.Sent)
        {
            document.SentAt = DateTime.UtcNow;
        }
        else if (toState == DocumentState.Confirmed)
        {
            document.ConfirmedAt = DateTime.UtcNow;
        }

        // 6. 상태 로그 생성 (분쟁 시 증빙)
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
            StateLogs = new List<DocumentStateLog> { stateLog }
        });
    }

    public IEnumerable<DocumentState> GetAllowedTransitions(DocumentState state)
    {
        if (!_allowedTransitions.ContainsKey(state))
            return Enumerable.Empty<DocumentState>();

        return _allowedTransitions[state];
    }
}
