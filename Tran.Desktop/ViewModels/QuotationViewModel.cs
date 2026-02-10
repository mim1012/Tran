using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Core.Services;
using Tran.Data;
using Tran.Data.Services;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 견적 화면 ViewModel (2-split: 작성 폼 + 미리보기)
/// 견적 작성, 전송, 확정 및 단가 변경 미리보기
/// </summary>
public class QuotationViewModel : ViewModelBase
{
    private string _companyId = string.Empty;
    private Quotation? _currentQuotation;
    private decimal _totalAmount;
    private string _statusMessage = string.Empty;

    public QuotationViewModel()
    {
        CurrentItems = new ObservableCollection<QuotationItemRow>();
        QuotationHistory = new ObservableCollection<Quotation>();
        PricePreview = new ObservableCollection<PriceChangePreview>();

        AddProductCommand = new RelayCommand<Product>(ExecuteAddProduct);
        RemoveItemCommand = new RelayCommand<QuotationItemRow>(ExecuteRemoveItem);
        SaveDraftCommand = new AsyncRelayCommand(ExecuteSaveDraftAsync);
        SendCommand = new AsyncRelayCommand(ExecuteSendAsync);
        ConfirmCommand = new AsyncRelayCommand(ExecuteConfirmAsync);

        CurrentItems.CollectionChanged += (_, _) =>
        {
            RecalculateTotal();
            _ = UpdatePricePreview();
        };
    }

    // ═══════════════════════════════════════════════════════════
    // Nested Helper Classes
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 견적 품목 행 (편집 가능, 기존 단가 비교 포함)
    /// </summary>
    public class QuotationItemRow : ViewModelBase
    {
        private int _productId;
        private string _productName = string.Empty;
        private decimal _quantity = 1;
        private decimal _unitPrice;
        private decimal _lineAmount;
        private string? _note;
        private decimal _currentPrice;

        public int ProductId
        {
            get => _productId;
            set => SetProperty(ref _productId, value);
        }

        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    LineAmount = _quantity * _unitPrice;
                }
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (SetProperty(ref _unitPrice, value))
                {
                    LineAmount = _quantity * _unitPrice;
                }
            }
        }

        public decimal LineAmount
        {
            get => _lineAmount;
            set => SetProperty(ref _lineAmount, value);
        }

        public string? Note
        {
            get => _note;
            set => SetProperty(ref _note, value);
        }

        /// <summary>
        /// 기존 거래처 단가 (비교용, 0이면 미등록)
        /// </summary>
        public decimal CurrentPrice
        {
            get => _currentPrice;
            set => SetProperty(ref _currentPrice, value);
        }
    }

    /// <summary>
    /// 단가 변경 미리보기 (확정 시 적용될 변경 사항)
    /// </summary>
    public class PriceChangePreview : ViewModelBase
    {
        private string _productName = string.Empty;
        private decimal _oldPrice;
        private decimal _newPrice;
        private decimal _priceChange;

        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        /// <summary>
        /// 기존 단가 (0이면 신규)
        /// </summary>
        public decimal OldPrice
        {
            get => _oldPrice;
            set => SetProperty(ref _oldPrice, value);
        }

        /// <summary>
        /// 새 단가
        /// </summary>
        public decimal NewPrice
        {
            get => _newPrice;
            set => SetProperty(ref _newPrice, value);
        }

        /// <summary>
        /// 가격 변동 (양수: 인상, 음수: 인하)
        /// </summary>
        public decimal PriceChange
        {
            get => _priceChange;
            set => SetProperty(ref _priceChange, value);
        }

        /// <summary>
        /// 기존 단가 별명 (XAML 바인딩용)
        /// </summary>
        public decimal CurrentPrice => OldPrice;

        /// <summary>
        /// 변동 방향 ("Up", "Down", "New")
        /// </summary>
        public string ChangeDirection => OldPrice == 0 ? "New" : PriceChange > 0 ? "Up" : PriceChange < 0 ? "Down" : "Same";

        /// <summary>
        /// 변동률 표시 텍스트
        /// </summary>
        public string ChangePercentDisplay => OldPrice == 0
            ? "신규"
            : $"{Math.Abs(PriceChange / OldPrice * 100):F1}%";

        /// <summary>
        /// 인상 여부
        /// </summary>
        public bool IsIncrease => PriceChange > 0;
    }

    // ═══════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 현재 거래처 ID
    /// </summary>
    public string CompanyId
    {
        get => _companyId;
        set => SetProperty(ref _companyId, value);
    }

    /// <summary>
    /// 현재 작성 중인 견적
    /// </summary>
    public Quotation? CurrentQuotation
    {
        get => _currentQuotation;
        set => SetProperty(ref _currentQuotation, value);
    }

    /// <summary>
    /// 현재 견적 품목 (편집 가능한 행 목록)
    /// </summary>
    public ObservableCollection<QuotationItemRow> CurrentItems { get; }

    /// <summary>
    /// 견적 이력 (하단 탭)
    /// </summary>
    public ObservableCollection<Quotation> QuotationHistory { get; }

    /// <summary>
    /// 단가 변경 미리보기 (확정 시 적용될 변경)
    /// </summary>
    public ObservableCollection<PriceChangePreview> PricePreview { get; }

    /// <summary>
    /// 합계 금액
    /// </summary>
    public decimal TotalAmount
    {
        get => _totalAmount;
        private set => SetProperty(ref _totalAmount, value);
    }

    /// <summary>
    /// 상태 메시지
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // ═══════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 품목 추가
    /// </summary>
    public ICommand AddProductCommand { get; }

    /// <summary>
    /// 품목 제거
    /// </summary>
    public ICommand RemoveItemCommand { get; }

    /// <summary>
    /// 임시 저장
    /// </summary>
    public ICommand SaveDraftCommand { get; }

    /// <summary>
    /// 견적 전송 (Draft -> Sent)
    /// </summary>
    public ICommand SendCommand { get; }

    /// <summary>
    /// 견적 확정 (-> CompanyPrice 업데이트)
    /// </summary>
    public ICommand ConfirmCommand { get; }

    // ═══════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 데이터 로드 (거래처 설정 후 호출)
    /// </summary>
    public async Task LoadDataAsync(string companyId)
    {
        CompanyId = companyId;

        try
        {
            using var context = CreateDbContext();

            // 견적 이력 로드
            var quotations = await context.Quotations
                .Include(q => q.Items)
                .Where(q => q.CompanyId == companyId)
                .OrderByDescending(q => q.QuotationDate)
                .Take(50)
                .ToListAsync();

            QuotationHistory.Clear();
            foreach (var quotation in quotations)
            {
                QuotationHistory.Add(quotation);
            }

            // 현재 견적 초기화
            UnsubscribeAllItems();
            CurrentItems.Clear();
            PricePreview.Clear();
            TotalAmount = 0;
        }
        catch (Exception ex)
        {
            StatusMessage = $"데이터 로드 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"QuotationViewModel 로드 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 단가 변경 미리보기 업데이트
    /// 현재 품목의 단가와 기존 CompanyPrice 비교
    /// </summary>
    public async Task UpdatePricePreview()
    {
        PricePreview.Clear();

        if (!CurrentItems.Any() || string.IsNullOrEmpty(CompanyId))
            return;

        try
        {
            using var context = CreateDbContext();
            var pricePolicyService = new PricePolicyService(context);

            foreach (var item in CurrentItems)
            {
                var currentPrice = await pricePolicyService.GetCompanyPriceAsync(CompanyId, item.ProductId);
                var oldPrice = currentPrice ?? 0;

                // 단가가 다른 경우만 표시
                if (oldPrice != item.UnitPrice)
                {
                    PricePreview.Add(new PriceChangePreview
                    {
                        ProductName = item.ProductName,
                        OldPrice = oldPrice,
                        NewPrice = item.UnitPrice,
                        PriceChange = item.UnitPrice - oldPrice
                    });
                }

                // 품목 행의 현재 단가도 업데이트
                item.CurrentPrice = oldPrice;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"단가 미리보기 업데이트 실패: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Event Handlers
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// QuotationItemRow.PropertyChanged 이벤트 핸들러 (명명된 메서드로 구독 해제 가능)
    /// </summary>
    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(QuotationItemRow.LineAmount))
        {
            RecalculateTotal();
        }
        if (args.PropertyName == nameof(QuotationItemRow.UnitPrice))
        {
            _ = UpdatePricePreview();
        }
    }

    /// <summary>
    /// 모든 현재 품목의 PropertyChanged 구독 해제
    /// </summary>
    private void UnsubscribeAllItems()
    {
        foreach (var item in CurrentItems)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════

    private TranDbContext CreateDbContext()
    {
        return DbContextFactory.Create();
    }

    private async void ExecuteAddProduct(Product? product)
    {
        if (product == null) return;

        try
        {
            // 이미 추가된 품목인지 확인
            var existing = CurrentItems.FirstOrDefault(i => i.ProductId == product.ProductId);
            if (existing != null)
            {
                existing.Quantity += 1;
                RecalculateTotal();
                return;
            }

            // 기존 거래처 단가 조회
            decimal currentPrice = 0;
            try
            {
                using var context = CreateDbContext();
                var pricePolicyService = new PricePolicyService(context);
                currentPrice = await pricePolicyService.GetCompanyPriceAsync(CompanyId, product.ProductId) ?? 0;
            }
            catch
            {
                // 단가 조회 실패 시 기본 단가 사용
            }

            var unitPrice = currentPrice > 0 ? currentPrice : product.DefaultPrice;

            var row = new QuotationItemRow
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Quantity = 1,
                UnitPrice = unitPrice,
                LineAmount = unitPrice,
                CurrentPrice = currentPrice
            };

            row.PropertyChanged += OnItemPropertyChanged;

            CurrentItems.Add(row);
        }
        catch (Exception ex)
        {
            StatusMessage = $"품목 추가 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"QuotationViewModel.ExecuteAddProduct 오류: {ex}");
        }
    }

    private void ExecuteRemoveItem(QuotationItemRow? item)
    {
        if (item == null) return;
        item.PropertyChanged -= OnItemPropertyChanged;
        CurrentItems.Remove(item);
    }

    private async Task ExecuteSaveDraftAsync()
    {
        if (!CurrentItems.Any())
        {
            StatusMessage = "품목을 추가해 주세요.";
            return;
        }

        try
        {
            using var context = CreateDbContext();
            var quotationService = new QuotationService(context);

            var quotation = BuildQuotationFromItems();
            var savedQuotation = await quotationService.CreateDraftAsync(quotation);

            CurrentQuotation = savedQuotation;
            StatusMessage = $"견적 임시 저장 완료 (#{savedQuotation.QuotationId})";

            // 이력에 추가
            QuotationHistory.Insert(0, savedQuotation);
        }
        catch (Exception ex)
        {
            StatusMessage = $"견적 저장 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"견적 저장 실패: {ex.Message}");
        }
    }

    private async Task ExecuteSendAsync()
    {
        if (CurrentQuotation == null)
        {
            // 먼저 저장
            await ExecuteSaveDraftAsync();
            if (CurrentQuotation == null) return;
        }

        try
        {
            using var context = CreateDbContext();
            var quotationService = new QuotationService(context);

            var sentQuotation = await quotationService.SendQuotationAsync(CurrentQuotation.QuotationId);

            CurrentQuotation = sentQuotation;
            StatusMessage = $"견적 #{sentQuotation.QuotationId} 전송 완료";
        }
        catch (Exception ex)
        {
            StatusMessage = $"견적 전송 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"견적 전송 실패: {ex.Message}");
        }
    }

    private async Task ExecuteConfirmAsync()
    {
        if (CurrentQuotation == null)
        {
            StatusMessage = "견적을 먼저 저장/전송해 주세요.";
            return;
        }

        try
        {
            using var context = CreateDbContext();
            var quotationService = new QuotationService(context);

            var confirmedQuotation = await quotationService.ConfirmQuotationAsync(CurrentQuotation.QuotationId);

            CurrentQuotation = confirmedQuotation;
            StatusMessage = $"견적 #{confirmedQuotation.QuotationId} 확정 완료 (단가 반영됨)";

            // 단가 미리보기 초기화 (이미 반영됨)
            PricePreview.Clear();

            // 새 견적 시작
            UnsubscribeAllItems();
            CurrentItems.Clear();
            TotalAmount = 0;
            CurrentQuotation = null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"견적 확정 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"견적 확정 실패: {ex.Message}");
        }
    }

    private Quotation BuildQuotationFromItems()
    {
        var quotation = new Quotation
        {
            CompanyId = CompanyId,
            QuotationDate = DateTime.Today,
            State = QuotationState.Draft,
            TotalAmount = TotalAmount,
            Items = CurrentItems.Select(row => new QuotationItem
            {
                ProductId = row.ProductId,
                ProductName = row.ProductName,
                Quantity = row.Quantity,
                UnitPrice = row.UnitPrice,
                LineAmount = row.LineAmount,
                Note = row.Note
            }).ToList()
        };
        return quotation;
    }

    private void RecalculateTotal()
    {
        TotalAmount = CurrentItems.Sum(i => i.LineAmount);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UnsubscribeAllItems();
        }
        base.Dispose(disposing);
    }
}
