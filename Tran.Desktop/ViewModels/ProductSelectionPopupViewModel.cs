using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Tran.Core.Models;
using Tran.Core.Services;
using Tran.Data;
using Tran.Data.Services;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 품목 선택 팝업 ViewModel (멀티 셀렉트 + 수량 일괄 입력)
/// 발주/판매/견적 화면에서 품목을 선택할 때 사용
/// </summary>
public class ProductSelectionPopupViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    private decimal _totalAmount;
    private string _companyId = string.Empty;
    private string _statusMessage = string.Empty;

    public ProductSelectionPopupViewModel()
    {
        AllProducts = new ObservableCollection<SelectableProduct>();
        FilteredProducts = new ObservableCollection<SelectableProduct>();
        SelectedProducts = new ObservableCollection<SelectedProductRow>();

        ToggleSelectionCommand = new RelayCommand<SelectableProduct>(ExecuteToggleSelection);
        ConfirmCommand = new RelayCommand(ExecuteConfirm, () => SelectedProducts.Any());
        CancelCommand = new RelayCommand(ExecuteCancel);
        SearchCommand = new RelayCommand(ExecuteSearch);
        FocusSearchCommand = new RelayCommand(() => { /* UI에서 Focus 처리 */ });

        SelectedProducts.CollectionChanged += (_, _) =>
        {
            RaisePropertyChanged(nameof(HasSelectedProducts));
            RaisePropertyChanged(nameof(SelectedProductCount));
        };
    }

    // ═══════════════════════════════════════════════════════════
    // Nested Helper Classes
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 선택 가능한 품목 (체크박스 포함)
    /// </summary>
    public class SelectableProduct : ViewModelBase
    {
        private bool _isSelected;

        /// <summary>
        /// 품목 데이터
        /// </summary>
        public Product Product { get; set; } = null!;

        /// <summary>
        /// 선택 여부
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        // Product 속성 편의 접근자
        public int ProductId => Product.ProductId;
        public string ProductName => Product.ProductName;
        public string? ProductCode => Product.ProductCode;
        public string? Category => Product.Category;
        public string Unit => Product.Unit;
        public decimal DefaultPrice => Product.DefaultPrice;
    }

    /// <summary>
    /// 선택된 품목 행 (수량 편집 가능)
    /// </summary>
    public class SelectedProductRow : ViewModelBase
    {
        private int _productId;
        private string _productName = string.Empty;
        private decimal _unitPrice;
        private decimal _quantity = 1;
        private decimal _lineAmount;
        private decimal? _companyPrice;

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

        /// <summary>
        /// 수량 (편집 가능)
        /// </summary>
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

        public decimal LineAmount
        {
            get => _lineAmount;
            set => SetProperty(ref _lineAmount, value);
        }

        /// <summary>
        /// 거래처 단가 (설정된 경우, null이면 미등록)
        /// </summary>
        public decimal? CompanyPrice
        {
            get => _companyPrice;
            set => SetProperty(ref _companyPrice, value);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 전체 품목 (선택 가능)
    /// </summary>
    public ObservableCollection<SelectableProduct> AllProducts { get; }

    /// <summary>
    /// 필터링된 품목 (검색 결과)
    /// </summary>
    public ObservableCollection<SelectableProduct> FilteredProducts { get; }

    /// <summary>
    /// 선택된 품목 (수량 입력 가능)
    /// </summary>
    public ObservableCollection<SelectedProductRow> SelectedProducts { get; }

    /// <summary>
    /// 검색어
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

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
    /// 품목 선택/해제 토글
    /// </summary>
    public ICommand ToggleSelectionCommand { get; }

    /// <summary>
    /// 확인 (선택 완료)
    /// </summary>
    public ICommand ConfirmCommand { get; }

    /// <summary>
    /// 취소
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// 검색
    /// </summary>
    public ICommand SearchCommand { get; }

    /// <summary>
    /// 검색 포커스 명령
    /// </summary>
    public ICommand FocusSearchCommand { get; }

    /// <summary>
    /// 선택된 품목 존재 여부
    /// </summary>
    public bool HasSelectedProducts => SelectedProducts.Count > 0;

    /// <summary>
    /// 선택된 품목 수
    /// </summary>
    public int SelectedProductCount => SelectedProducts.Count;

    // ═══════════════════════════════════════════════════════════
    // Events / Actions
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 확인 시 콜백 (선택된 품목 목록 전달)
    /// </summary>
    public Action<List<SelectedProductRow>>? OnConfirmed { get; set; }

    /// <summary>
    /// 취소 시 콜백
    /// </summary>
    public Action? OnCancelled { get; set; }

    // ═══════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 품목 데이터 로드 (거래처별 단가 포함)
    /// </summary>
    public async Task LoadProductsAsync(string companyId)
    {
        _companyId = companyId;

        try
        {
            using var context = CreateDbContext();
            var productService = new ProductService(context);
            var pricePolicyService = new PricePolicyService(context);

            // 전체 활성 품목 로드
            var products = await productService.GetAllProductsAsync(includeInactive: false);

            // 거래처별 단가 로드
            var companyPrices = await pricePolicyService.GetCompanyPricesAsync(companyId);
            var priceMap = companyPrices.ToDictionary(cp => cp.ProductId, cp => cp.UnitPrice);

            AllProducts.Clear();
            foreach (var product in products)
            {
                var selectable = new SelectableProduct
                {
                    Product = product,
                    IsSelected = false
                };
                selectable.PropertyChanged += OnSelectableProductPropertyChanged;
                AllProducts.Add(selectable);
            }

            // _companyPriceMap 저장 (선택 시 사용)
            _companyPriceMap = priceMap;

            ApplyFilter();
        }
        catch (Exception ex)
        {
            StatusMessage = $"품목 로드 실패: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"ProductSelectionPopup 로드 실패: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════

    private Dictionary<int, decimal> _companyPriceMap = new();

    private TranDbContext CreateDbContext()
    {
        return DbContextFactory.Create();
    }

    /// <summary>
    /// 필터 적용 (검색어 기반)
    /// </summary>
    private void ApplyFilter()
    {
        FilteredProducts.Clear();

        var filtered = AllProducts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var keyword = SearchText.Trim().ToLower();
            filtered = filtered.Where(sp =>
                sp.ProductName.ToLower().Contains(keyword) ||
                (sp.ProductCode?.ToLower().Contains(keyword) ?? false) ||
                (sp.Category?.ToLower().Contains(keyword) ?? false));
        }

        foreach (var product in filtered)
        {
            FilteredProducts.Add(product);
        }
    }

    /// <summary>
    /// SelectableProduct.IsSelected 변경 시 SelectedProducts 동기화
    /// </summary>
    private void OnSelectableProductPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(SelectableProduct.IsSelected) && sender is SelectableProduct selectable)
        {
            SyncSelectedProduct(selectable);
        }
    }

    /// <summary>
    /// IsSelected 상태에 따라 SelectedProducts에 추가/제거
    /// </summary>
    private void SyncSelectedProduct(SelectableProduct selectable)
    {
        if (selectable.IsSelected)
        {
            // 이미 추가되어 있으면 skip
            if (SelectedProducts.Any(r => r.ProductId == selectable.ProductId))
                return;

            var companyPrice = _companyPriceMap.TryGetValue(selectable.ProductId, out var price)
                ? (decimal?)price
                : null;
            var unitPrice = companyPrice ?? selectable.DefaultPrice;

            var row = new SelectedProductRow
            {
                ProductId = selectable.ProductId,
                ProductName = selectable.ProductName,
                UnitPrice = unitPrice,
                Quantity = 1,
                LineAmount = unitPrice,
                CompanyPrice = companyPrice
            };
            row.PropertyChanged += OnSelectedProductPropertyChanged;
            SelectedProducts.Add(row);
        }
        else
        {
            var toRemove = SelectedProducts.FirstOrDefault(r => r.ProductId == selectable.ProductId);
            if (toRemove != null)
            {
                toRemove.PropertyChanged -= OnSelectedProductPropertyChanged;
                SelectedProducts.Remove(toRemove);
            }
        }

        RecalculateTotal();
        CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    /// 품목 선택/해제 토글 (커맨드용)
    /// </summary>
    private void ExecuteToggleSelection(SelectableProduct? selectable)
    {
        if (selectable == null) return;

        selectable.IsSelected = !selectable.IsSelected;

        if (selectable.IsSelected)
        {
            // 선택: SelectedProducts에 추가
            var companyPrice = _companyPriceMap.TryGetValue(selectable.ProductId, out var price)
                ? (decimal?)price
                : null;

            var unitPrice = companyPrice ?? selectable.DefaultPrice;

            var row = new SelectedProductRow
            {
                ProductId = selectable.ProductId,
                ProductName = selectable.ProductName,
                UnitPrice = unitPrice,
                Quantity = 1,
                LineAmount = unitPrice,
                CompanyPrice = companyPrice
            };

            row.PropertyChanged += OnSelectedProductPropertyChanged;

            SelectedProducts.Add(row);
        }
        else
        {
            // 해제: SelectedProducts에서 제거 (구독 해제 포함)
            var toRemove = SelectedProducts.FirstOrDefault(r => r.ProductId == selectable.ProductId);
            if (toRemove != null)
            {
                toRemove.PropertyChanged -= OnSelectedProductPropertyChanged;
                SelectedProducts.Remove(toRemove);
            }
        }

        RecalculateTotal();
        CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    /// 확인 실행
    /// </summary>
    private void ExecuteConfirm()
    {
        var selectedList = SelectedProducts.ToList();
        OnConfirmed?.Invoke(selectedList);
    }

    /// <summary>
    /// 취소 실행
    /// </summary>
    private void ExecuteCancel()
    {
        OnCancelled?.Invoke();
    }

    /// <summary>
    /// 검색 실행
    /// </summary>
    private void ExecuteSearch()
    {
        ApplyFilter();
    }

    /// <summary>
    /// SelectedProductRow.PropertyChanged 이벤트 핸들러 (명명된 메서드로 구독 해제 가능)
    /// </summary>
    private void OnSelectedProductPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(SelectedProductRow.LineAmount))
        {
            RecalculateTotal();
        }
    }

    /// <summary>
    /// 모든 선택된 품목의 PropertyChanged 구독 해제
    /// </summary>
    private void UnsubscribeAllSelectedProducts()
    {
        foreach (var row in SelectedProducts)
        {
            row.PropertyChanged -= OnSelectedProductPropertyChanged;
        }
    }

    /// <summary>
    /// 합계 재계산
    /// </summary>
    private void RecalculateTotal()
    {
        TotalAmount = SelectedProducts.Sum(r => r.LineAmount);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UnsubscribeAllSelectedProducts();
            foreach (var sp in AllProducts)
            {
                sp.PropertyChanged -= OnSelectableProductPropertyChanged;
            }
        }
        base.Dispose(disposing);
    }
}
