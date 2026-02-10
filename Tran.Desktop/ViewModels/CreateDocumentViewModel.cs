using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Tran.Core.Models;
using Tran.Core.Utilities;
using Tran.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 거래명세표 작성 화면 ViewModel
/// Draft 상태로 문서 생성 및 품목 입력 관리
/// </summary>
public class CreateDocumentViewModel : ViewModelBase
{
    private Company? _selectedFromCompany;
    private Company? _selectedToCompany;
    private DateTime _transactionDate = DateTime.Today;

    public CreateDocumentViewModel()
    {
        _ = LoadCompaniesAsync();

        // Commands 초기화
        AddItemCommand = new RelayCommand(OnAddItem);
        RemoveItemCommand = new RelayCommand<DocumentItemViewModel>(OnRemoveItem);
        EditSpecCommand = new RelayCommand<DocumentItemViewModel>(OnEditSpec);
        SaveDraftCommand = new AsyncRelayCommand(OnSaveDraftAsync, CanSave);
        SaveAndSendCommand = new AsyncRelayCommand(OnSaveAndSendAsync, CanSave);
        CancelCommand = new RelayCommand<Window>(OnCancel);

        // 품목 변경 감지를 위한 이벤트 구독
        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    /// <summary>
    /// Items 컬렉션 변경 시 이벤트 핸들러
    /// 새 품목의 PropertyChanged 구독 및 제거된 품목의 구독 해제
    /// </summary>
    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RaisePropertyChanged(nameof(TotalAmount));

        // Command CanExecute 재평가 (품목 추가/삭제 시 저장 버튼 활성화)
        CommandManager.InvalidateRequerySuggested();

        // 새 품목의 PropertyChanged 이벤트 구독
        if (e.NewItems != null)
        {
            foreach (DocumentItemViewModel item in e.NewItems)
            {
                item.PropertyChanged += OnDocumentItemPropertyChanged;
            }
        }

        // 제거된 품목의 PropertyChanged 이벤트 구독 해제
        if (e.OldItems != null)
        {
            foreach (DocumentItemViewModel item in e.OldItems)
            {
                item.PropertyChanged -= OnDocumentItemPropertyChanged;
            }
        }

        // Reset 시 (Clear 호출 시) 기존 구독 해제는 Dispose에서 처리
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            // Reset은 OldItems가 null이므로,
            // Clear 전에 UnsubscribeAllDocumentItems()를 명시적으로 호출해야 함
        }
    }

    /// <summary>
    /// DocumentItemViewModel.PropertyChanged 이벤트 핸들러
    /// </summary>
    private void OnDocumentItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RaisePropertyChanged(nameof(TotalAmount));
    }

    /// <summary>
    /// 모든 품목의 PropertyChanged 구독 해제
    /// </summary>
    private void UnsubscribeAllDocumentItems()
    {
        foreach (var item in Items)
        {
            item.PropertyChanged -= OnDocumentItemPropertyChanged;
        }
    }

    #region Properties

    /// <summary>
    /// 발신 거래처 목록
    /// </summary>
    public ObservableCollection<Company> AvailableFromCompanies { get; } = new();

    /// <summary>
    /// 수신 거래처 목록
    /// </summary>
    public ObservableCollection<Company> AvailableToCompanies { get; } = new();

    /// <summary>
    /// 선택된 발신 거래처
    /// </summary>
    public Company? SelectedFromCompany
    {
        get => _selectedFromCompany;
        set
        {
            if (SetProperty(ref _selectedFromCompany, value))
            {
                // Command CanExecute 재평가 트리거
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// 선택된 수신 거래처
    /// </summary>
    public Company? SelectedToCompany
    {
        get => _selectedToCompany;
        set
        {
            if (SetProperty(ref _selectedToCompany, value))
            {
                // Command CanExecute 재평가 트리거
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// 거래일자
    /// </summary>
    public DateTime TransactionDate
    {
        get => _transactionDate;
        set => SetProperty(ref _transactionDate, value);
    }

    /// <summary>
    /// 품목 목록
    /// </summary>
    public ObservableCollection<DocumentItemViewModel> Items { get; } = new();

    /// <summary>
    /// 총 금액 (품목 금액 합계)
    /// </summary>
    public decimal TotalAmount => Items.Sum(item => item.LineAmount);

    #endregion

    #region Commands

    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand EditSpecCommand { get; }
    public ICommand SaveDraftCommand { get; }
    public ICommand SaveAndSendCommand { get; }
    public ICommand CancelCommand { get; }

    private void OnAddItem()
    {
        var newItem = new DocumentItemViewModel
        {
            ItemName = "새 품목",
            OptionText = "",
            Quantity = 1,
            UnitPrice = 0
        };

        Items.Add(newItem);
    }

    private void OnRemoveItem(DocumentItemViewModel? item)
    {
        if (item != null)
        {
            Items.Remove(item);
        }
    }

    private void OnEditSpec(DocumentItemViewModel? item)
    {
        if (item == null) return;

        // SpecEditorViewModel 생성 (품목 ViewModel 전달)
        var editorViewModel = new SpecEditorViewModel(item);

        // SpecEditorWindow 표시
        var window = new SpecEditorWindow(editorViewModel)
        {
            Owner = Application.Current.Windows.OfType<CreateDocumentWindow>().FirstOrDefault()
        };

        // 모달 다이얼로그로 표시
        window.ShowDialog();
    }

    private bool CanSave()
    {
        // 필수 필드 검증
        return SelectedFromCompany != null
            && SelectedToCompany != null
            && Items.Count > 0;
    }

    private Task OnSaveDraftAsync()
    {
        return SaveDocumentAsync(DocumentState.Draft, sendAfterSave: false);
    }

    private Task OnSaveAndSendAsync()
    {
        return SaveDocumentAsync(DocumentState.Draft, sendAfterSave: true);
    }

    private void OnCancel(Window? window)
    {
        var result = MessageBox.Show(
            "작성 중인 내용이 저장되지 않습니다. 취소하시겠습니까?",
            "취소 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            window?.Close();
        }
    }

    #endregion

    #region Private Methods

    private async Task LoadCompaniesAsync()
    {
        using var context = DbContextFactory.Create();

        var companies = await context.Companies.ToListAsync();

        foreach (var company in companies)
        {
            AvailableFromCompanies.Add(company);
            AvailableToCompanies.Add(company);
        }

        // 첫 번째 회사를 기본 발신 거래처로 설정
        if (AvailableFromCompanies.Count > 0)
        {
            SelectedFromCompany = AvailableFromCompanies[0];
        }
    }

    private async Task SaveDocumentAsync(DocumentState initialState, bool sendAfterSave)
    {
        try
        {
            if (SelectedFromCompany == null || SelectedToCompany == null)
            {
                MessageBox.Show("발신 거래처와 수신 거래처를 선택해주세요.", "입력 오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Items.Count == 0)
            {
                MessageBox.Show("최소 1개 이상의 품목을 추가해주세요.", "입력 오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var context = DbContextFactory.Create();

            // 문서 번호 생성 + 저장 (중복 시 최대 3회 재시도)
            Document document = null!;
            const int maxRetries = 3;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                var documentId = await GenerateDocumentIdAsync(context);
                if (attempt > 0)
                {
                    documentId += $"-R{attempt}";
                }

                // DocumentItem 목록 생성
                var documentItems = Items.Select((item, index) =>
                {
                    // 규격 데이터를 Canonical JSON으로 변환
                    var canonicalSpec = SpecCanonicalizer.Canonicalize(item.Specs);
                    var specJson = SpecCanonicalizer.ToJson(canonicalSpec);

                    return new DocumentItem
                    {
                        ItemId = $"{documentId}-ITEM-{(index + 1):D3}",
                        DocumentId = documentId,
                        ItemName = item.ItemName,
                        OptionText = item.OptionText,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        LineAmount = item.LineAmount,
                        ExtraDataJson = specJson  // 규격 데이터 저장 (Canonical JSON)
                    };
                }).ToList();

                // ContentHash 계산
                var contentHash = CalculateContentHash(documentItems);

                // Document 생성
                document = new Document
                {
                    DocumentId = documentId,
                    FromCompanyId = SelectedFromCompany.CompanyId,
                    ToCompanyId = SelectedToCompany.CompanyId,
                    TransactionDate = TransactionDate,
                    State = initialState,
                    StateVersion = 0,
                    VersionNumber = 1,
                    ContentHash = contentHash,
                    TotalAmount = TotalAmount,
                    CreatedBy = Tran.Core.Services.UserContext.CurrentUserId,
                    CreatedAt = DateTime.UtcNow
                };

                // DB 저장 (Document와 Items 별도 추가)
                context.Documents.Add(document);
                context.DocumentItems.AddRange(documentItems);

                try
                {
                    await context.SaveChangesAsync();
                    break; // 저장 성공
                }
                catch (DbUpdateException) when (attempt < maxRetries - 1)
                {
                    // PK 중복 충돌 - 엔티티 분리 후 재시도
                    context.ChangeTracker.Clear();
                    continue;
                }
            }

            // 저장 후 전송: Draft → Sent 상태 전이 수행
            string message;
            if (sendAfterSave)
            {
                var transitionService = new Tran.Core.Services.StateTransitionService();
                var transitionResult = await transitionService.TransitionAsync(
                    document, DocumentState.Sent, Tran.Core.Services.UserContext.CurrentUserId, "저장 후 자동 전송");

                if (transitionResult.Success)
                {
                    context.Documents.Update(document);
                    if (transitionResult.StateLogs.Count > 0)
                    {
                        context.DocumentStateLogs.AddRange(transitionResult.StateLogs);
                    }
                    await context.SaveChangesAsync();
                    message = $"문서가 전송되었습니다. (#{document.DocumentId})";
                }
                else
                {
                    message = $"문서가 저장되었으나 전송에 실패했습니다: {transitionResult.ErrorMessage}";
                }
            }
            else
            {
                message = "문서가 임시 저장되었습니다.";
            }

            MessageBox.Show(message, "저장 완료",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // 창 닫기
            Application.Current.Windows
                .OfType<CreateDocumentWindow>()
                .FirstOrDefault()?.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"문서 저장 중 오류가 발생했습니다: {ex.Message}", "오류",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task<string> GenerateDocumentIdAsync(TranDbContext context)
    {
        var year = DateTime.Now.Year;
        var prefix = $"DOC-{year}-";

        // DB-level MAX 집계: 메모리에 전체 ID를 로드하지 않음
        var maxNumber = await context.Documents
            .Where(d => d.DocumentId.StartsWith(prefix))
            .Select(d => d.DocumentId.Substring(prefix.Length, 4))
            .Where(s => s.Length == 4)
            .MaxAsync(s => (int?)Convert.ToInt32(s)) ?? 0;

        var nextNumber = maxNumber + 1;
        return $"{prefix}{nextNumber:D4}";
    }

    private string CalculateContentHash(List<DocumentItem> items)
    {
        // 품목 데이터를 JSON으로 직렬화
        // ✅ CRITICAL: 규격(ExtraDataJson)도 해시에 포함 - 분쟁 방지의 핵심
        var itemsData = items.Select(item => new
        {
            item.ItemName,
            item.OptionText,
            item.Quantity,
            item.UnitPrice,
            item.LineAmount,
            Specs = item.ExtraDataJson ?? string.Empty  // 규격 데이터 포함 (Canonical JSON)
        }).OrderBy(x => x.ItemName).ThenBy(x => x.OptionText);

        var json = JsonSerializer.Serialize(itemsData);
        var bytes = Encoding.UTF8.GetBytes(json);

        // SHA-256 해시 계산
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UnsubscribeAllDocumentItems();
            Items.CollectionChanged -= OnItemsCollectionChanged;
        }
        base.Dispose(disposing);
    }
}
