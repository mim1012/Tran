using System.Collections.ObjectModel;
using System.Windows.Input;
using Tran.Core.Models;
using Tran.Core.Services;
using Tran.Core.Utilities;
using Microsoft.EntityFrameworkCore;
using Tran.Data;
using System.Windows;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 거래명세표 상세 화면 ViewModel
/// 상태에 따른 UI 권한 제어 및 상태 전이 Command 제공
/// </summary>
public class DocumentDetailViewModel : ViewModelBase
{
    private Document _document;
    private DocumentState _state;
    private readonly IStateTransitionService _stateTransitionService;
    private readonly string _currentUser = "SYSTEM"; // TODO: 실제 로그인 사용자 정보

    public DocumentDetailViewModel(string documentId)
    {
        _stateTransitionService = new StateTransitionService();
        LoadDocumentAsync(documentId).GetAwaiter().GetResult();

        // Commands 초기화
        SendCommand = new RelayCommand(OnSend, () => CanSend);
        ConfirmCommand = new RelayCommand(OnConfirm, () => CanConfirm);
        RequestRevisionCommand = new RelayCommand(OnRequestRevision, () => CanRequestRevision);
        CloseCommand = new RelayCommand<Window>(OnClose);
    }

    #region Document Properties

    public string DocumentId => _document.DocumentId;
    public string FromCompanyId => _document.FromCompanyId;
    public string ToCompanyId => _document.ToCompanyId;
    public decimal TotalAmount => _document.TotalAmount;
    public DateTime TransactionDate => _document.TransactionDate;
    public int VersionNumber => _document.VersionNumber;
    public DateTime CreatedAt => _document.CreatedAt;

    /// <summary>
    /// 품목 목록
    /// </summary>
    public ObservableCollection<DocumentItemDisplay> Items { get; } = new();

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
                RaiseAllPermissionProperties();
            }
        }
    }

    #endregion

    #region UI Permission Booleans (PRD Table 4)

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
    /// 편집 가능 여부 (Draft만)
    /// </summary>
    public bool CanEdit => State == DocumentState.Draft;

    /// <summary>
    /// 정산 포함 가능 여부 (Confirmed만)
    /// </summary>
    public bool CanIncludeInSettlement => State == DocumentState.Confirmed;

    #endregion

    #region Display Properties

    /// <summary>
    /// 상태 표시 텍스트
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
    /// 상태 Pill 배경색
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
    /// 상태 Pill 텍스트 색상
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

    #endregion

    #region Commands

    public ICommand SendCommand { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand RequestRevisionCommand { get; }
    public ICommand CloseCommand { get; }

    private async void OnSend()
    {
        var result = MessageBox.Show(
            "이 문서를 전송하시겠습니까?",
            "전송 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await TransitionStateAsync(DocumentState.Sent, "전송");
        }
    }

    private async void OnConfirm()
    {
        var result = MessageBox.Show(
            "이 문서를 확정하시겠습니까? 확정 후에는 수정할 수 없습니다.",
            "확정 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            await TransitionStateAsync(DocumentState.Confirmed, "확정");
        }
    }

    private async void OnRequestRevision()
    {
        // TODO: 수정 요청 사유 입력 창
        await TransitionStateAsync(DocumentState.RevisionRequested, "수정 요청");
    }

    private void OnClose(Window? window)
    {
        window?.Close();
    }

    #endregion

    #region Private Methods

    private async Task LoadDocumentAsync(string documentId)
    {
        var options = new DbContextOptionsBuilder<TranDbContext>()
            .UseSqlite("Data Source=tran.db")
            .Options;

        using var context = new TranDbContext(options);

        _document = await context.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == documentId)
            ?? throw new InvalidOperationException($"Document not found: {documentId}");

        _state = _document.State;

        // 품목 목록 로드
        var items = await context.DocumentItems
            .Where(i => i.DocumentId == documentId)
            .OrderBy(i => i.ItemId)
            .ToListAsync();

        // Items 컬렉션에 추가
        foreach (var item in items)
        {
            Items.Add(DocumentItemDisplay.FromDocumentItem(item));
        }
    }

    /// <summary>
    /// 상태 전이 실행 및 DB 저장
    /// </summary>
    private async Task TransitionStateAsync(DocumentState toState, string actionName)
    {
        try
        {
            // 1. StateTransitionService를 통한 상태 전이
            var transitionResult = await _stateTransitionService.TransitionAsync(
                _document,
                toState,
                _currentUser,
                $"{actionName} 작업");

            if (!transitionResult.Success)
            {
                MessageBox.Show(
                    transitionResult.ErrorMessage ?? "상태 전이에 실패했습니다.",
                    "오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // 2. DB에 저장
            var options = new DbContextOptionsBuilder<TranDbContext>()
                .UseSqlite("Data Source=tran.db")
                .Options;

            using var context = new TranDbContext(options);

            // Document 업데이트
            context.Documents.Attach(_document);
            context.Entry(_document).State = EntityState.Modified;

            // StateLog 저장
            if (transitionResult.StateLog != null)
            {
                context.DocumentStateLogs.Add(transitionResult.StateLog);
            }

            await context.SaveChangesAsync();

            // 3. UI 업데이트
            State = toState;

            // 4. 성공 메시지
            MessageBox.Show(
                $"문서가 {actionName}되었습니다.",
                $"{actionName} 완료",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"상태 전이 중 오류가 발생했습니다: {ex.Message}",
                "오류",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void RaiseAllPermissionProperties()
    {
        RaisePropertyChanged(nameof(CanSend));
        RaisePropertyChanged(nameof(CanConfirm));
        RaisePropertyChanged(nameof(CanRequestRevision));
        RaisePropertyChanged(nameof(CanEdit));
        RaisePropertyChanged(nameof(CanIncludeInSettlement));
        RaisePropertyChanged(nameof(StateDisplayText));
        RaisePropertyChanged(nameof(StatePillBackground));
        RaisePropertyChanged(nameof(StatePillForeground));
    }

    #endregion

    #region Nested Classes

    /// <summary>
    /// 품목 표시용 ViewModel
    /// </summary>
    public class DocumentItemDisplay
    {
        public string ItemName { get; set; } = string.Empty;
        public string OptionText { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineAmount { get; set; }

        /// <summary>
        /// 규격 요약 텍스트 (Canonical JSON을 파싱하여 표시)
        /// </summary>
        public string SpecSummary { get; set; } = string.Empty;

        /// <summary>
        /// 규격 상세 (툴팁용)
        /// </summary>
        public string SpecTooltip { get; set; } = string.Empty;

        public static DocumentItemDisplay FromDocumentItem(DocumentItem item)
        {
            var specs = SpecCanonicalizer.FromJson(item.ExtraDataJson);

            var specSummary = specs.Count == 0
                ? "-"
                : $"규격 {specs.Count}";

            var specTooltip = specs.Count == 0
                ? "규격 없음"
                : string.Join("\n", specs.Select(s => $"{s.Key}: {s.Value}"));

            return new DocumentItemDisplay
            {
                ItemName = item.ItemName,
                OptionText = item.OptionText ?? string.Empty,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineAmount = item.LineAmount,
                SpecSummary = specSummary,
                SpecTooltip = specTooltip
            };
        }
    }

    #endregion
}
