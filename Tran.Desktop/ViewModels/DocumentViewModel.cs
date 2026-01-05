using Tran.Core.Models;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 거래명세표 ViewModel
/// "State → ViewModel → UI는 단방향이다"
/// PRD의 State → Boolean 매핑 규칙을 엄격히 준수
/// </summary>
public class DocumentViewModel : ViewModelBase
{
    private Document _document;
    private DocumentState _state;

    public DocumentViewModel(Document document)
    {
        _document = document;
        _state = document.State;
    }

    /// <summary>
    /// 현재 문서 상태
    /// </summary>
    public DocumentState State
    {
        get => _state;
        private set
        {
            if (SetProperty(ref _state, value))
            {
                // 모든 Boolean 속성 갱신
                RaiseAllPermissionProperties();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // UI 권한 Boolean (PRD Table 4의 매핑 규칙 그대로 구현)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 입력 가능 여부 (Draft만)
    /// </summary>
    public bool CanEdit => State == DocumentState.Draft;

    /// <summary>
    /// 품목 편집 가능 여부
    /// </summary>
    public bool CanEditItems => State == DocumentState.Draft;

    /// <summary>
    /// 전송 가능 여부 (Draft만)
    /// </summary>
    public bool CanSend => State == DocumentState.Draft;

    /// <summary>
    /// 확정 가능 여부 (Received만)
    /// </summary>
    public bool CanConfirm => State == DocumentState.Received;

    /// <summary>
    /// 수정 요청 가능 여부 (Received만)
    /// </summary>
    public bool CanRequestRevision => State == DocumentState.Received;

    /// <summary>
    /// 새 버전 생성 가능 여부 (RevisionRequested만)
    /// </summary>
    public bool CanCreateNewVersion => State == DocumentState.RevisionRequested;

    /// <summary>
    /// 출력 가능 여부 (모든 상태)
    /// </summary>
    public bool CanPrint => true;

    /// <summary>
    /// 정산 포함 가능 여부 (Confirmed만)
    /// </summary>
    public bool CanIncludeInSettlement => State == DocumentState.Confirmed;

    /// <summary>
    /// 삭제 가능 여부 (Draft만)
    /// </summary>
    public bool CanDelete => State == DocumentState.Draft;

    // ═══════════════════════════════════════════════════════════
    // 문서 정보 속성
    // ═══════════════════════════════════════════════════════════

    public string DocumentId => _document.DocumentId;
    public string FromCompanyId => _document.FromCompanyId;
    public string ToCompanyId => _document.ToCompanyId;
    public decimal TotalAmount => _document.TotalAmount;
    public DateTime TransactionDate => _document.TransactionDate;
    public int VersionNumber => _document.VersionNumber;

    /// <summary>
    /// 상태 Pill 배경색 (Enterprise B2B 스타일)
    /// </summary>
    public string StatePillBackground => State switch
    {
        DocumentState.Draft => "#F0F0F0",
        DocumentState.Sent => "#E8F1FF",
        DocumentState.Received => "#FFF4E5",
        DocumentState.RevisionRequested => "#FFE5E5",
        DocumentState.Confirmed => "#E6F4EA",
        DocumentState.Superseded => "#F5F5F5",
        DocumentState.Cancelled => "#FFE5E5",
        _ => "#F0F0F0"
    };

    /// <summary>
    /// 상태 Pill 텍스트 색상 (Enterprise B2B 스타일)
    /// </summary>
    public string StatePillForeground => State switch
    {
        DocumentState.Draft => "#555555",
        DocumentState.Sent => "#1E5EFF",
        DocumentState.Received => "#E67700",
        DocumentState.RevisionRequested => "#C92A2A",
        DocumentState.Confirmed => "#1E7F34",
        DocumentState.Superseded => "#868E96",
        DocumentState.Cancelled => "#C92A2A",
        _ => "#555555"
    };

    /// <summary>
    /// 상태 표시 텍스트 (한글)
    /// </summary>
    public string StateDisplayText => State switch
    {
        DocumentState.Draft => "작성중",
        DocumentState.Sent => "전송됨",
        DocumentState.Received => "수신됨",
        DocumentState.RevisionRequested => "수정요청",
        DocumentState.Confirmed => "확정됨",
        DocumentState.Superseded => "구버전",
        DocumentState.Cancelled => "취소됨",
        _ => "알 수 없음"
    };

    /// <summary>
    /// 상태 변경 시 모든 Boolean 속성 갱신
    /// </summary>
    private void RaiseAllPermissionProperties()
    {
        RaisePropertyChanged(nameof(CanEdit));
        RaisePropertyChanged(nameof(CanEditItems));
        RaisePropertyChanged(nameof(CanSend));
        RaisePropertyChanged(nameof(CanConfirm));
        RaisePropertyChanged(nameof(CanRequestRevision));
        RaisePropertyChanged(nameof(CanCreateNewVersion));
        RaisePropertyChanged(nameof(CanPrint));
        RaisePropertyChanged(nameof(CanIncludeInSettlement));
        RaisePropertyChanged(nameof(CanDelete));
        RaisePropertyChanged(nameof(StatePillBackground));
        RaisePropertyChanged(nameof(StatePillForeground));
        RaisePropertyChanged(nameof(StateDisplayText));
    }

    /// <summary>
    /// 상태 업데이트 (외부에서 상태 전이 후 호출)
    /// </summary>
    public void UpdateState(DocumentState newState)
    {
        State = newState;
    }
}
