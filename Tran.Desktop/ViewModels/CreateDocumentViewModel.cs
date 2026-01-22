using System.Collections.ObjectModel;
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
        LoadCompaniesAsync().GetAwaiter().GetResult();

        // Commands 초기화
        AddItemCommand = new RelayCommand(OnAddItem);
        RemoveItemCommand = new RelayCommand<DocumentItemViewModel>(OnRemoveItem);
        EditSpecCommand = new RelayCommand<DocumentItemViewModel>(OnEditSpec);
        SaveDraftCommand = new RelayCommand(OnSaveDraft, CanSave);
        SaveAndSendCommand = new RelayCommand(OnSaveAndSend, CanSave);
        CancelCommand = new RelayCommand<Window>(OnCancel);

        // 품목 변경 감지를 위한 이벤트 구독
        Items.CollectionChanged += (s, e) =>
        {
            RaisePropertyChanged(nameof(TotalAmount));

            // Command CanExecute 재평가 (품목 추가/삭제 시 저장 버튼 활성화)
            CommandManager.InvalidateRequerySuggested();

            // 기존 품목의 PropertyChanged 이벤트 구독
            if (e.NewItems != null)
            {
                foreach (DocumentItemViewModel item in e.NewItems)
                {
                    item.PropertyChanged += (_, __) => RaisePropertyChanged(nameof(TotalAmount));
                }
            }
        };
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

    private async void OnSaveDraft()
    {
        await SaveDocumentAsync(DocumentState.Draft, sendAfterSave: false);
    }

    private async void OnSaveAndSend()
    {
        await SaveDocumentAsync(DocumentState.Draft, sendAfterSave: true);
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
        var options = new DbContextOptionsBuilder<TranDbContext>()
            .UseSqlite("Data Source=tran.db")
            .Options;

        using var context = new TranDbContext(options);

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

            var options = new DbContextOptionsBuilder<TranDbContext>()
                .UseSqlite("Data Source=tran.db")
                .Options;

            using var context = new TranDbContext(options);

            // 문서 번호 생성
            var documentId = await GenerateDocumentIdAsync(context);

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
            var document = new Document
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
                CreatedAt = DateTime.UtcNow
            };

            // DB 저장 (Document와 Items 별도 추가)
            context.Documents.Add(document);
            context.DocumentItems.AddRange(documentItems);
            await context.SaveChangesAsync();

            // 성공 메시지
            var message = sendAfterSave
                ? "문서가 저장되었습니다. 이제 전송할 수 있습니다."
                : "문서가 임시 저장되었습니다.";

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

        // 해당 연도의 마지막 문서 번호 조회
        var lastDocument = await context.Documents
            .Where(d => d.DocumentId.StartsWith(prefix))
            .OrderByDescending(d => d.DocumentId)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastDocument != null)
        {
            var lastNumberStr = lastDocument.DocumentId.Replace(prefix, "");
            if (int.TryParse(lastNumberStr, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

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
}
